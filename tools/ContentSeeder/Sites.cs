using Imprint.Authoring.Domain;

namespace ContentSeeder;

/// <summary>
/// A navigation entry the CMS header declares (sites/*/lib/site.ts). <see cref="Href"/>
/// is the CMS href exactly as written; on-site entries (relative hrefs) become Imprint
/// NavigationItems, off-site entries are recorded but not representable in Imprint's
/// same-site-page navigation model (flagged, never invented).
/// </summary>
public sealed record NavEntry(string Label, string Href);

/// <summary>
/// One target site: the CMS content source, the Imprint site/home ids to author into,
/// the public origin (used to absolutise site-relative links so they stay valid canonical
/// hrefs), the brand key for widget accents, and the header navigation from site.ts.
/// </summary>
public sealed record SiteDef(
    string Key,
    string BrandName,
    string Origin,
    string CmsDir,
    SiteId SiteId,
    PageId HomePageId,
    IReadOnlyList<NavEntry> HeaderNav);

public static class Sites
{
    // The three Imprint sites (prod ids mirrored locally for the verify DB).
    public static readonly SiteId WatchdogSite = SiteId.From(Guid.Parse("63030ae7-aeb9-4355-a019-362c150fd420"));
    public static readonly PageId WatchdogHome = PageId.From(Guid.Parse("1e4fad3b-fb44-44f0-a73e-174f0f975396"));

    public static readonly SiteId CaiSite = SiteId.From(Guid.Parse("c3236bdf-87db-403e-bc06-379649f7a2d8"));
    public static readonly PageId CaiHome = PageId.From(Guid.Parse("889aff8a-f2a9-4bb6-b2fc-2050f77e5924"));

    public static readonly SiteId AssaySite = SiteId.From(Guid.Parse("91884b35-d865-42a8-98bd-c7960fce6f89"));
    public static readonly PageId AssayHome = PageId.From(Guid.Parse("e192c652-7e28-44c7-8674-aba7de5860ca"));

    public static IReadOnlyList<SiteDef> All(string cmsRoot) =>
    [
        new SiteDef("watchdog", "Watchdog", "https://watchdog.canine.dev",
            Path.Combine(cmsRoot, "sites", "watchdog", "content"),
            WatchdogSite, WatchdogHome, WatchdogNav),
        new SiteDef("assay", "Assay", "https://assay.canine.dev",
            Path.Combine(cmsRoot, "sites", "assay", "content"),
            AssaySite, AssayHome, AssayNav),
        new SiteDef("cai", "CAI", "https://cai.canine.dev",
            Path.Combine(cmsRoot, "sites", "cai", "content"),
            CaiSite, CaiHome, CaiNav),
    ];

    // ── Header navigation, lifted verbatim from sites/*/lib/site.ts ──────────────
    // Imprint navigation is a flat list of same-site page links. The CMS "menu"
    // dropdowns are flattened to their child links in declaration order; grouping,
    // desc lines, header CTAs, footer groups, theme toggle and copyLine are chrome
    // Imprint's static publisher does not model (flagged in the report, never invented).

    private static readonly IReadOnlyList<NavEntry> WatchdogNav =
    [
        new("What we measure", "/methodology"),
        new("Where Watchdog fits", "/where-watchdog-fits"),
        new("Agents & MCP", "/agents"),
        new("Evidence you can share", "/evidence"),
        new("Public reports", "/publicreports"),
        new("Framework catalog", "/catalog"),
        new("Compliance", "/compliance"),
        new("Security & data", "/security"),
        new("Freelancers & solo devs", "/for-freelancers"),
        new("Engineering teams", "/for-teams"),
        new("Providers & consultancies", "/for-consultancies"),
        new("Builders", "/for-builders"),
        new("Leads", "/for-leads"),
        new("Pricing", "/pricing"),
    ];

    private static readonly IReadOnlyList<NavEntry> AssayNav =
    [
        new("How it works", "/how-it-works"),
        new("All reports", "/reports"),
        new("Consequences report", "/reports/consequences"),
        new("Due-diligence dossier", "/reports/due-diligence"),
        new("Tender & delivery verification", "/reports/tender"),
        new("Contract appendix & attestation", "/reports/attestation"),
        new("Compliance & signing pack", "/reports/compliance"),
        new("Portfolio appraisal", "/reports/portfolio"),
        new("Buyers & procurement", "/for-buyers"),
        new("Acquirers, investors & insurers", "/for-acquirers"),
        new("Software owners", "/for-owners"),
        new("Compliance & regulated", "/for-compliance"),
        new("Decision-makers", "/for-decision-makers"),
        new("Pricing", "/pricing"),
    ];

    private static readonly IReadOnlyList<NavEntry> CaiNav =
    [
        new("The standard", "/spec"),
        new("Dimensions & lenses", "/dimensions"),
        new("Rubric versions", "/rubric"),
        new("Verify", "/verify"),
        new("Registry", "/registry"),
        new("Scorer / CLI", "/cli"),
    ];
}
