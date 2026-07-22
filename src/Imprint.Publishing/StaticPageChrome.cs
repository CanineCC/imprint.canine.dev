namespace Imprint.Publishing;

/// <summary>
/// Everything <see cref="StaticPageDocument"/> needs around the page content: head
/// metadata, the marketing chrome (brand, grouped nav, header actions, footer columns,
/// copy line) and which of the two inline scripts to emit. Computed by the publisher per
/// page × locale — every href is already resolved to a concrete URL for this locale and
/// every label to a concrete string — so the component stays a dumb template.
/// </summary>
public sealed record StaticPageChrome
{
    public required string Lang { get; init; }

    /// <summary>The full document title (page title · site name).</summary>
    public required string Title { get; init; }

    public string? MetaDescription { get; init; }

    /// <summary>Null suppresses the canonical link (the 404 page has no canonical URL).</summary>
    public string? CanonicalHref { get; init; }

    public IReadOnlyList<Alternate> Alternates { get; init; } = [];

    public required string StylesheetHref { get; init; }

    public required string SiteName { get; init; }

    /// <summary>Where the site-name link points — the home page in the current locale.</summary>
    public required string HomeHref { get; init; }

    public IReadOnlyList<NavItem> Nav { get; init; } = [];

    /// <summary>The header's primary call-to-action button, or null when the site sets none.</summary>
    public HeaderLink? HeaderCta { get; init; }

    /// <summary>The header's quiet secondary link (e.g. "Sign in"), or null when unset.</summary>
    public HeaderLink? HeaderQuiet { get; init; }

    /// <summary>The footer's named link columns; empty renders the minimal footer.</summary>
    public IReadOnlyList<FooterColumn> FooterGroups { get; init; } = [];

    /// <summary>The footer's fine-print copy line, or null when the site sets none.</summary>
    public string? CopyLine { get; init; }

    /// <summary>
    /// The resolved published <c>/assets/…</c> URL of the site's favicon (a small variant),
    /// or null when the site sets none — then no <c>&lt;link rel="icon"&gt;</c> is emitted.
    /// </summary>
    public string? FaviconUrl { get; init; }

    /// <summary>
    /// The resolved published <c>/assets/…</c> URL of the site's header logo (a header-height
    /// variant), or null — then the brand falls back to the CSS <c>.ip-brand-dot</c>.
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// True only when the rendered content carries <c>data-island</c> markup — decided
    /// from the page tree (a widget renders an island exactly when its manifest entry
    /// and bundle resolve), so no second render pass is needed.
    /// </summary>
    public bool IncludeIslandLoader { get; init; }

    public sealed record Alternate(string Hreflang, string Href);

    /// <summary>
    /// One top-level nav entry: EITHER a direct link (<see cref="Href"/> set,
    /// <see cref="Children"/> empty) OR a dropdown group (<see cref="Children"/>
    /// non-empty, <see cref="Href"/> null). <see cref="IsCurrent"/> marks the active page.
    /// </summary>
    public sealed record NavItem(
        string Label,
        string? Href,
        bool IsCurrent,
        IReadOnlyList<NavChild> Children)
    {
        public bool IsGroup => Children.Count > 0;
    }

    /// <summary>A dropdown-card link: label, resolved href, an optional supporting line, active flag.</summary>
    public sealed record NavChild(string Label, string Href, string? Description, bool IsCurrent);

    /// <summary>A resolved header action (label + concrete href).</summary>
    public sealed record HeaderLink(string Label, string Href);

    /// <summary>A resolved footer column (heading + its resolved links).</summary>
    public sealed record FooterColumn(string Heading, IReadOnlyList<FooterEntry> Links);

    public sealed record FooterEntry(string Label, string Href);
}
