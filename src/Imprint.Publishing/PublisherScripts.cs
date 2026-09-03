using System.Text;

namespace Imprint.Publishing;

/// <summary>
/// The only pieces of platform JavaScript a published page may carry
/// (docs/publishing.md §The HTML contract), read verbatim from the embedded
/// <c>assets/*.js</c> files — the publisher inlines them, it never rewrites them.
/// </summary>
public static class PublisherScripts
{
    /// <summary>
    /// Theme override (~15 lines). Inlined into &lt;head&gt; before the stylesheet —
    /// blocking by design so an explicit light/dark choice applies at first paint.
    /// </summary>
    public static string ThemeToggle { get; } = Load("theme-toggle.js");

    /// <summary>
    /// Language preference (~20 lines). Inlined into &lt;head&gt; beside the theme override and for the same
    /// reason: it must decide before first paint, or a visitor sees the wrong language flash past. It reads
    /// the page's own hreflang alternates, so it costs nothing and does nothing on a single-locale site.
    /// </summary>
    public static string LanguagePreference { get; } = Load("language-preference.js");

    /// <summary>
    /// Mobile nav toggle (~45 lines), inlined at the end of &lt;body&gt; on every page — below
    /// the header breakpoint the nav is otherwise unreachable. A checkbox hack would drop
    /// keyboard users and &lt;details&gt; cannot show its content closed on desktop, so this
    /// is the honest minimum: one button opens the sheet, and the groups inside it are
    /// accordions (one open at a time) because hover opens nothing on a finger. It owns
    /// aria-expanded on the group triggers below the breakpoint and removes it above,
    /// where the panels are hover-driven and the attribute would be a lie.
    /// </summary>
    public static string NavToggle { get; } = Load("nav-toggle.js");

    /// <summary>
    /// Island loader (~1 KB), inlined at the end of &lt;body&gt; — it queries
    /// <c>[data-island]</c> synchronously, so it must run after the islands exist in
    /// the DOM. Only emitted on pages that actually contain islands.
    /// </summary>
    public static string IslandLoader { get; } = Load("island-loader.js");

    private static string Load(string name)
    {
        using var stream = typeof(PublisherScripts).Assembly
            .GetManifestResourceStream($"Imprint.Publishing.assets.{name}")
            ?? throw new InvalidOperationException($"Embedded script '{name}' is missing from Imprint.Publishing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
