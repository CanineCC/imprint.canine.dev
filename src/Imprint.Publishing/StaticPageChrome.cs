namespace Imprint.Publishing;

/// <summary>
/// Everything <see cref="StaticPageDocument"/> needs around the page content: head
/// metadata, the navigation, and which of the two inline scripts to emit. Computed by
/// the publisher per page × locale; the component stays a dumb template.
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

    public IReadOnlyList<NavLink> Nav { get; init; } = [];

    /// <summary>
    /// True only when the rendered content carries <c>data-island</c> markup — decided
    /// from the page tree (a widget renders an island exactly when its manifest entry
    /// and bundle resolve), so no second render pass is needed.
    /// </summary>
    public bool IncludeIslandLoader { get; init; }

    public sealed record Alternate(string Hreflang, string Href);

    public sealed record NavLink(string Label, string Href, bool IsCurrent);
}
