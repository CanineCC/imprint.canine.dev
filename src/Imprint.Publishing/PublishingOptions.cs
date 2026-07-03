namespace Imprint.Publishing;

/// <summary>Where the publisher writes, where widget bundles come from, and how it addresses the site.</summary>
public sealed record PublishingOptions
{
    /// <summary>The output directory — the file-system projection's "store".</summary>
    public required string OutputPath { get; init; }

    /// <summary>Directory containing <c>manifest.json</c> and the widget ES-module bundles.</summary>
    public required string WidgetsDirectory { get; init; }

    /// <summary>
    /// Absolute site origin (e.g. <c>https://example.com</c>) used for canonical URLs,
    /// hreflang alternates and sitemap <c>&lt;loc&gt;</c> entries. When null those fall
    /// back to root-relative paths — the trade-off: the output stays origin-agnostic
    /// (move hosts without republishing, valid per RFC 3986 relative references), but
    /// some crawlers ignore relative canonicals and cross-host duplicate detection
    /// weakens. Set it for production; leave it null for local preview.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Quiet period after the last projection catch-up before the hosted service
    /// republishes, so a theme-editing session doesn't re-render per keystroke.
    /// </summary>
    public int DebounceMilliseconds { get; init; } = 2000;
}
