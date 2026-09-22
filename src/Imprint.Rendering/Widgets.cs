using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imprint.Rendering;

/// <summary>
/// A widget as described by <c>widgets/manifest.json</c>: a custom element the static
/// page can carry with zero platform JavaScript. Everything the editor needs (a typed
/// prop form, a placeholder) and everything the publisher needs (the bundle to copy)
/// lives in the manifest — adding a widget requires no C#.
/// </summary>
/// <summary>
/// Resolves a widget URL template — <c>{prop}</c> tokens against an instance's declared prop values.
/// <para>ONE implementation on purpose. The view uses it for the pre-hydration fallback link and for
/// finding this instance's publish-time bake; the publisher uses it to decide what to fetch and to
/// key the result. If those two ever resolved the same template differently, the publisher would
/// bake under one URL and the view would look under another, and the bake would silently never
/// appear — a failure that looks exactly like the feature being off.</para>
/// </summary>
public static class WidgetTemplate
{
    /// <summary>
    /// The resolved absolute https URL, or null. Null when: there is no template; a token names a
    /// prop the manifest does not declare, or declares <see cref="WidgetProp.Private"/>, or the
    /// instance leaves empty; a token is left unresolved; or the result is not absolute https.
    /// An unresolved token is a broken promise rather than a partial URL, so it yields nothing.
    /// </summary>
    /// <summary>
    /// The cache key for a publish-time bake: the URL it was fetched from AND the template that
    /// rendered it.
    /// <para>★ Both halves are load-bearing. Two widgets can read ONE endpoint and render different
    /// parts of it — a pricing page does exactly that, with the packages in one section and the
    /// self-hosted rows in another. Keyed by URL alone the second overwrites the first, one rendering
    /// is produced, and both sections look it up.</para>
    /// <para>It lives beside <see cref="Resolve"/> for the same reason Resolve does: the publisher
    /// writes this key and the view reads it, and a key built twice is a key that can differ.</para>
    /// </summary>
    public static string BakeKey(string url, string? template, string? context = null) =>
        context is { Length: > 0 } ? $"{url}\n{template}\n{context}" : $"{url}\n{template}";

    /// <summary>
    /// The descriptor's context url for these props — or "" when it declares none.
    /// </summary>
    /// <remarks>
    /// ★★ ONE FUNCTION, BECAUSE THE BAKE AND THE LOOKUP MUST AGREE EXACTLY. They are in different
    /// assemblies and run at different times; if either derives this key differently the lookup
    /// misses, the widget silently publishes its fallback, and nothing says why.
    /// <para>★ AND IT TAKES THE PROPS. The first attempt passed none, on the reasoning that a
    /// context is shared by every instance and so cannot depend on one — but the pattern is
    /// <c>{base}/api/public/reports</c> and <c>base</c> IS a prop. It resolved to null, no context
    /// was ever fetched, and every card drew the flat bar exactly as before, with a green deploy and
    /// a green suite. Shared means the pattern names nothing per-instance, not that props are
    /// withheld from it.</para>
    /// </remarks>
    public static string ContextUrl(WidgetDescriptor descriptor, Func<string, string?> props) =>
        descriptor.PrerenderContext is { Length: > 0 } pattern
            ? Resolve(descriptor, pattern, props) ?? ""
            : "";

    public static string? Resolve(WidgetDescriptor descriptor, string? template, Func<string, string?> value)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(value);
        if (template is not { Length: > 0 } resolved)
        {
            return null;
        }

        foreach (var prop in descriptor.Props)
        {
            var token = "{" + prop.Name + "}";
            if (!resolved.Contains(token, StringComparison.Ordinal))
            {
                continue;
            }

            if (prop.Private || value(prop.Name) is not { Length: > 0 } filled)
            {
                return null;
            }

            resolved = resolved.Replace(token, filled.TrimEnd('/'), StringComparison.Ordinal);
        }

        return !resolved.Contains('{', StringComparison.Ordinal)
            && Uri.TryCreate(resolved, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
          ? uri.ToString()
          : null;
    }
}

public sealed record WidgetDescriptor
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";

    /// <summary>Relative bundle path inside the widgets directory, e.g. <c>x-countdown.js</c>.</summary>
    public string Bundle { get; init; } = "";

    /// <summary>CSS aspect-ratio (e.g. <c>16 / 9</c>) reserved before hydration — zero layout shift.</summary>
    public string? AspectRatio { get; init; }

    /// <summary>Text shown inside the element before hydration and in the editor placeholder.</summary>
    public string Placeholder { get; init; } = "";

    /// <summary>
    /// Optional href TEMPLATE for the pre-hydration fallback — <c>{prop}</c> tokens are substituted
    /// with the instance's declared prop values (e.g. <c>{base}/embed/{view}</c>). When every token
    /// resolves and the result is an absolute https URL, the static fallback carries a real link to
    /// the live view — so a visitor without JavaScript (and a crawler) gets a working path to the
    /// data instead of a dead label. Anything unresolved or non-https renders no link at all.
    /// </summary>
    public string? FallbackHref { get; init; }

    /// <summary>
    /// Optional URL TEMPLATE, resolved exactly like <see cref="FallbackHref"/>, whose response is
    /// fetched AT PUBLISH TIME and baked into the element's light DOM as machine-readable markup.
    /// <para>Why it exists: a widget that renders itself in a shadow root (or an iframe inside one)
    /// puts nothing in the published HTML. A crawler, an LLM, or a reader without JavaScript then
    /// sees an empty custom element — which is how a pricing page came to carry 7,700 characters and
    /// not one price. The fetched fragment is reduced to a semantic subset (see
    /// <c>WidgetPrerender</c>) and emitted as the element's children, so the facts are in the
    /// document even though the live view is what a browser displays.</para>
    /// <para>A fetch that fails changes nothing: the element renders exactly as it does without this
    /// field. A stale bake is the cost — the live view is always current, the baked copy is as old as
    /// the last publish.</para>
    /// </summary>
    public string? Prerender { get; init; }

    /// <summary>
    /// Names a SERVER-SIDE template that turns the <see cref="Prerender"/> response into this site's
    /// own markup at publish time — e.g. <c>pricing</c>.
    /// <para>Declaring one changes the widget's nature: the fetched body is DATA, the rendered markup
    /// is the element's visible content, and no island is emitted, so nothing hydrates and no iframe
    /// is ever constructed. The element becomes a build-time include whose source of truth is another
    /// service and whose presentation is entirely ours.</para>
    /// <para>Without a template the fetched body is treated as HTML and reduced to a hidden,
    /// machine-readable copy beside a live island — a different trade with a different purpose.</para>
    /// </summary>
    public string? PrerenderTemplate { get; init; }

    /// <summary>
    /// A SECOND url whose body is handed to the template alongside <see cref="Prerender"/>'s.
    /// </summary>
    /// <remarks>
    /// <para>★★ WHY A CARD NEEDS TWO. The published-survey card wants the score, the history and the
    /// lenses — which one feed carries — and the BAND CUTLINES, which decide which word and which
    /// hue a score gets. The cutlines are scoring data: they belong to the rubric, this site must
    /// never hold a hand-written copy (see <c>BandScaleTemplate</c>), and the feed carrying the
    /// history does not publish them. With one fetch the card had to choose which half to go
    /// without, and it drew a flat grey bar on every site for want of four numbers that are public
    /// at a different address.</para>
    /// <para>★ IT IS CONTEXT, NOT A SECOND SUBJECT. The first url decides WHAT the widget is about;
    /// this one only supplies shared facts that qualify it. So it takes no props and is the same for
    /// every instance — which means one fetch for the whole site, not one per card.</para>
    /// <para>The bake key covers both, so two widgets differing only in context cannot share a
    /// fragment. A context that does not answer changes nothing: the template renders from the first
    /// body alone, exactly as it did before.</para>
    /// </remarks>
    public string? PrerenderContext { get; init; }

    /// <summary>
    /// Names a server-side template that renders this widget from its OWN PROPS, with no fetch.
    /// </summary>
    /// <remarks>
    /// <para>★ THE DIFFERENCE FROM <see cref="PrerenderTemplate"/> IS WHERE THE DATA LIVES, AND IT
    /// DECIDES WHERE THE RENDERING CAN HAPPEN. A prerendered widget's data comes from a URL, so it is
    /// baked once at publish time and cached by (url, template) — which is exactly why that cache
    /// cannot see props: one fragment is shared by every instance reading that URL. A prop-driven
    /// widget has no URL and nothing to share; its content is authored on the node. It is therefore
    /// rendered HERE, at page-render time, where the props are in hand and no cache is involved.</para>
    /// <para>The effect on the element is the same as a bake: real markup inside it, no island, no
    /// script, nothing to hydrate — and the same <c>ip-widget-baked</c> marker, because every
    /// stylesheet rule that has to tell content from a loading placeholder asks that question.</para>
    /// </remarks>
    public string? PropTemplate { get; init; }

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
