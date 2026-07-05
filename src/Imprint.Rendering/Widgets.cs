using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imprint.Rendering;

/// <summary>
/// A widget as described by <c>widgets/manifest.json</c>: a custom element the static
/// page can carry with zero platform JavaScript. Everything the editor needs (a typed
/// prop form, a placeholder) and everything the publisher needs (the bundle to copy)
/// lives in the manifest — adding a widget requires no C#.
/// </summary>
public sealed record WidgetDescriptor
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";

    /// <summary>Relative bundle path inside the widgets directory, e.g. <c>x-countdown.js</c>.</summary>
    public required string Bundle { get; init; }

    /// <summary>CSS aspect-ratio (e.g. <c>16 / 9</c>) reserved before hydration — zero layout shift.</summary>
    public string? AspectRatio { get; init; }

    /// <summary>Text shown inside the element before hydration and in the editor placeholder.</summary>
    public string Placeholder { get; init; } = "";

    /// <summary>Hydrate immediately instead of on approach (for above-the-fold widgets).</summary>
    public bool Eager { get; init; }

    public IReadOnlyList<WidgetProp> Props { get; init; } = [];
}

public sealed record WidgetProp
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public WidgetPropType Type { get; init; } = WidgetPropType.Text;
    public string? Default { get; init; }

    /// <summary>For <see cref="WidgetPropType.Choice"/>.</summary>
    public IReadOnlyList<string> Options { get; init; } = [];

    /// <summary>
    /// Server-side only: the editor shows the prop and the value lives in the node's
    /// prop bag, but <c>WidgetView</c> never emits it as an attribute — in any render
    /// mode — so it can carry data the published page must not reveal (e.g. the
    /// contact-form's inbox addresses, read live by the /api/contact endpoint).
    /// </summary>
    public bool Private { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WidgetPropType { Text, Number, Color, Url, Choice, Toggle }

/// <summary>Loads and validates <c>widgets/manifest.json</c>.</summary>
public static class WidgetManifest
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<WidgetDescriptor> Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        var descriptors = JsonSerializer.Deserialize<List<WidgetDescriptor>>(File.ReadAllText(manifestPath), Options) ?? [];
        foreach (var descriptor in descriptors)
        {
            // Custom-element tags must contain a hyphen and stay lower-case ASCII —
            // this is also what keeps the tag safe to emit into HTML unescaped.
            if (!IsValidTag(descriptor.Tag))
            {
                throw new InvalidOperationException(
                    $"Widget tag '{descriptor.Tag}' is invalid: custom-element tags are lower-case ASCII with at least one hyphen.");
            }

            // Prop names become HTML attribute names verbatim, so an on*/style name would
            // ship a live event handler / inline style to visitors. Reject a malformed
            // (or hostile) built-in manifest loudly rather than dropping the prop silently.
            foreach (var prop in descriptor.Props)
            {
                if (!IsValidPropName(prop.Name))
                {
                    throw new InvalidOperationException(
                        $"Widget '{descriptor.Tag}' declares an invalid prop name '{prop.Name}': prop names are lower-case ASCII data attributes, never an event handler (on…) or 'style'.");
                }
            }
        }

        return descriptors;
    }

    public static bool IsValidTag(string tag) =>
        tag.Length is > 2 and <= 64 &&
        tag.Contains('-', StringComparison.Ordinal) &&
        char.IsAsciiLetterLower(tag[0]) &&
        tag.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    /// <summary>
    /// Prop names become HTML attribute NAMES emitted verbatim (Blazor encodes attribute
    /// values, not names), so they must be plain data attributes: lower-case ASCII, and
    /// NEVER an HTML event handler or <c>style</c> — either would turn an author-controlled
    /// value into live script / inline CSS on every visitor's page (the same denial
    /// SvgPublishGuard applies to inlined SVG). Keep in sync with the domain copy in
    /// <c>WidgetSubmission.IsValidPropName</c>.
    /// </summary>
    public static bool IsValidPropName(string name) =>
        name.Length is > 0 and <= 64 &&
        char.IsAsciiLetterLower(name[0]) &&
        name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-') &&
        !IsReservedAttributeName(name);

    // A prop name is reserved when WidgetView itself emits an attribute of that name with
    // security-sensitive meaning, so an author-controlled prop must never be allowed to
    // shadow it: `style` (inline CSS) and the whole `data-island*` namespace, whose
    // `data-island` value island-loader.js imports as a module URL — a prop with that name
    // would be emitted as a duplicate attribute the browser resolves to the AUTHOR's value,
    // running attacker-chosen JavaScript on every visitor (bypassing admin bundle review).
    // Also every HTML event handler: "on" followed by an all-letter event name (onclick,
    // onmouseover, …). Matching that shape — not any name merely starting with "on" —
    // blocks every real handler while still allowing innocuous names like "only-item".
    internal static bool IsReservedAttributeName(string name)
    {
        if (name == "style" || name.StartsWith("data-island", StringComparison.Ordinal))
        {
            return true;
        }

        if (name.Length <= 2 || name[0] != 'o' || name[1] != 'n')
        {
            return false;
        }

        for (var i = 2; i < name.Length; i++)
        {
            if (!char.IsAsciiLetterLower(name[i]))
            {
                return false;
            }
        }

        return true;
    }
}
