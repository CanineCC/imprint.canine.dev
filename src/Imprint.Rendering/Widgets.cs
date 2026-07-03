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
        }

        return descriptors;
    }

    public static bool IsValidTag(string tag) =>
        tag.Length is > 2 and <= 64 &&
        tag.Contains('-', StringComparison.Ordinal) &&
        char.IsAsciiLetterLower(tag[0]) &&
        tag.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    /// <summary>Prop names become HTML attribute names; keep them boring on purpose.</summary>
    public static bool IsValidPropName(string name) =>
        name.Length is > 0 and <= 64 &&
        char.IsAsciiLetterLower(name[0]) &&
        name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
}
