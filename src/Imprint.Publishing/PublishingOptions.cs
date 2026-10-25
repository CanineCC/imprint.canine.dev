namespace Imprint.Publishing;

/// <summary>Where the publisher writes, where widget bundles come from, and how it addresses the site.</summary>
public sealed record PublishingOptions
{
    /// <summary>
    /// The default output directory — the file-system projection's "store" for the
    /// single-site (no environments configured) fallback. In multi-site SaaS use each
    /// site publishes to its own environment folders instead; this stays the home of the
    /// first site until it is given environments.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// When set, every site's environment folder is treated as a path <em>relative to
    /// this root</em>, and anything resolving outside it is rejected — the sandbox that
    /// keeps one tenant (or a typo) from writing over another's files or the system's.
    /// When null, environment folders are used as absolute paths as-is: correct for a
    /// single trusted operator publishing into their own web roots, which is the shape
    /// of the initial SaaS deployment.
    /// </summary>
    public string? DeployRoot { get; init; }

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

    /// <summary>
    /// How often the hosted service re-publishes with no event at all, so a page's baked data from another service (a price list) is
    /// re-fetched even when that service's "look again" (<c>/api/authoring/notify</c>) was lost or blocked. Content-addressed, so a
    /// pass where nothing moved rewrites nothing. 0 turns it off. Ten minutes: the signal is the fast path, this is the safety net.
    /// </summary>
    public int RefreshMilliseconds { get; init; } = 600_000;
}
