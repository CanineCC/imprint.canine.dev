using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;

namespace ContentSeeder;

/// <summary>
/// A single navigation/footer link the CMS chrome declares (sites/*/lib/site.ts).
/// <see cref="Href"/> is the CMS href exactly as written (a same-site relative path or a
/// cross-site absolute URL); <see cref="Desc"/> is the optional dropdown supporting line.
/// The Migrator resolves the href to an Imprint <c>PageLink</c> when it names a migrated
/// same-site page, otherwise to an <c>ExternalLink</c> against the site's public origin —
/// nothing is invented, the link simply keeps its real deployed destination.
/// </summary>
public sealed record NavLink(string Label, string Href, string? Desc = null);

/// <summary>
/// One top-level header entry: EITHER a direct link (<see cref="Href"/> set) OR a dropdown
/// group (<see cref="Children"/> non-empty) — mirroring the CMS <c>NavItem</c> shape.
/// </summary>
public sealed record NavEntry(string Label, string? Href = null, IReadOnlyList<NavLink>? Children = null)
{
    public bool IsGroup => Children is { Count: > 0 };
}

/// <summary>A named footer link column (heading + its links).</summary>
public sealed record FooterCol(string Heading, IReadOnlyList<NavLink> Links);

/// <summary>A header action (CTA or quiet link): a label and its CMS href.</summary>
public sealed record HeaderAct(string Label, string Href);

/// <summary>
/// One target site: the CMS content source, the Imprint site/home ids to author into,
/// the public origin (used to absolutise site-relative links so they stay valid canonical
/// hrefs), the brand key for widget accents, and the full site.ts chrome — header nav
/// (with groups), the header CTA + quiet link, the footer columns and the copy line.
/// </summary>
public sealed record SiteDef(
    string Key,
    string BrandName,
    string Origin,
    string CmsDir,
    SiteId SiteId,
    PageId HomePageId,
    IReadOnlyList<NavEntry> HeaderNav,
    HeaderAct HeaderCta,
    HeaderAct? HeaderQuiet,
    IReadOnlyList<FooterCol> FooterGroups,
    string CopyLine,
    Themes.AccentRamp Accent,
    // The rest of the per-site brand layer: its neutral family and its typography. Split
    // out so a site can wear its own palette + type voice without any other site's
    // generated tokens changing (assay wears "Dal"; watchdog + cai keep the shared look).
    IReadOnlyDictionary<string, Themes.Tok> Neutrals,
    Typography Typography);

public static class Sites
{
    // The four Imprint sites (prod ids mirrored locally for the verify DB; the canine
    // ids are fixed constants minted here — prod inherits them at first seed).
    public static readonly SiteId WatchdogSite = SiteId.From(Guid.Parse("63030ae7-aeb9-4355-a019-362c150fd420"));
    public static readonly PageId WatchdogHome = PageId.From(Guid.Parse("1e4fad3b-fb44-44f0-a73e-174f0f975396"));

    public static readonly SiteId CaiSite = SiteId.From(Guid.Parse("c3236bdf-87db-403e-bc06-379649f7a2d8"));
    public static readonly PageId CaiHome = PageId.From(Guid.Parse("889aff8a-f2a9-4bb6-b2fc-2050f77e5924"));

    public static readonly SiteId AssaySite = SiteId.From(Guid.Parse("91884b35-d865-42a8-98bd-c7960fce6f89"));
    public static readonly PageId AssayHome = PageId.From(Guid.Parse("e192c652-7e28-44c7-8674-aba7de5860ca"));

    public static readonly SiteId CanineSite = SiteId.From(Guid.Parse("30d6c379-2712-4e26-9204-6abdc716f5eb"));
    public static readonly PageId CanineHome = PageId.From(Guid.Parse("2e800859-de7e-4daf-a969-bbe9a3f9473a"));

    /// <param name="cmsRoot">The cms.canine.dev checkout (watchdog / assay / cai content).</param>
    /// <param name="repoRoot">This repo's root — the canine studio site's content lives IN
    /// this repo (tools/ContentSeeder/canine/content), not in the CMS checkout, because its
    /// source of truth is the hand-written canine.dev Blazor site, transcribed once into the
    /// same CMS content shape so the whole Migrator/BlockMapper/Verify pipeline applies.</param>
    public static IReadOnlyList<SiteDef> All(string cmsRoot, string repoRoot) =>
    [
        new SiteDef("watchdog", "Watchdog", "https://watchdog.canine.dev",
            Path.Combine(cmsRoot, "sites", "watchdog", "content"),
            WatchdogSite, WatchdogHome,
            WatchdogNav, WatchdogCta, WatchdogQuiet, WatchdogFooter, WatchdogCopy,
            Themes.Watchdog, Themes.Neutrals, Themes.Marketing),
        new SiteDef("assay", "Assay", "https://assay.canine.dev",
            Path.Combine(cmsRoot, "sites", "assay", "content"),
            AssaySite, AssayHome,
            AssayNav, AssayCta, AssayQuiet, AssayFooter, AssayCopy,
            // "Dal": the warm-paper neutrals + copper accent + editorial serif voice.
            Themes.Assay, Themes.AssayPaper, Themes.Editorial),
        new SiteDef("cai", "CAI", "https://cai.canine.dev",
            Path.Combine(cmsRoot, "sites", "cai", "content"),
            CaiSite, CaiHome,
            CaiNav, CaiCta, CaiQuiet, CaiFooter, CaiCopy,
            Themes.Cai, Themes.Neutrals, Themes.Marketing),
        new SiteDef("canine", "Canine", "https://www.canine.dev",
            Path.Combine(repoRoot, "tools", "ContentSeeder", "canine", "content"),
            CanineSite, CanineHome,
            CanineNav, CanineCta, CanineQuiet, CanineFooter, CanineCopy,
            // canine.dev wears the family default look verbatim: its app.css tokens ARE the
            // shared graphite/paper neutrals + the steel accent (canine :root), same type.
            Themes.Watchdog, Themes.Neutrals, Themes.Marketing),
    ];

    // ── Chrome, lifted verbatim from sites/*/lib/site.ts. The header nav preserves the
    //    dropdown grouping + desc lines; the footer columns, header CTA + quiet link and
    //    copyLine are represented too — all now first-class in Imprint's site model. ──

    // ── watchdog ──────────────────────────────────────────────────────────────────
    private static readonly IReadOnlyList<NavEntry> WatchdogNav =
    [
        new("Why Watchdog", Children:
        [
            new("What we measure", "/methodology", "The lenses behind the index"),
            new("Where Watchdog fits", "/where-watchdog-fits", "Alongside your tools, one altitude up"),
            new("Agents & MCP", "/agents", "Hand findings to your coding agent"),
            new("Evidence you can share", "/evidence", "A signed package both sides trust"),
            new("Public reports", "/publicreports", "Real, falsifiable surveys"),
            new("Framework catalog", "/catalog", "Ten frameworks, measured honestly"),
            new("Compliance", "/compliance", "We measure; you declare"),
            new("Security & data", "/security", "EU residency, read-only, no third-party AI"),
        ]),
        new("Who it's for", Children:
        [
            new("Freelancers & solo devs", "/for-freelancers", "Prove your code, team of one"),
            new("Engineering teams", "/for-teams", "A scheduled audit you can trend"),
            new("Providers & consultancies", "/for-consultancies", "Win the bid with proof"),
            new("Builders", "/for-builders", "Fix the code, keep the score green"),
            new("Leads", "/for-leads", "Where to point the team this sprint"),
        ]),
        new("Pricing", "/pricing"),
    ];

    private static readonly HeaderAct WatchdogCta = new("Survey a repo — free", "https://app.watchdog.canine.dev/");
    private static readonly HeaderAct? WatchdogQuiet = new("Sign in", "https://app.watchdog.canine.dev/");
    private const string WatchdogCopy = "© 2025–2026 · The independent surveyor for C#/.NET software.";

    private static readonly IReadOnlyList<FooterCol> WatchdogFooter =
    [
        new("Product",
        [
            new("What we measure", "/methodology"),
            new("Where Watchdog fits", "/where-watchdog-fits"),
            new("Agents & MCP", "/agents"),
            new("Evidence & sharing", "/evidence"),
            new("Framework catalog", "/catalog"),
            new("Compliance", "/compliance"),
            new("Accessibility conformance", "/accessibility-conformance"),
            new("Pricing", "/pricing"),
            new("Public reports", "/publicreports"),
        ]),
        new("Who it's for",
        [
            new("Freelancers & solo devs", "/for-freelancers"),
            new("Engineering teams", "/for-teams"),
            new("Providers & consultancies", "/for-consultancies"),
            new("Builders", "/for-builders"),
            new("Leads", "/for-leads"),
        ]),
        new("The CAI standard",
        [
            new("cai.canine.dev", "https://cai.canine.dev"),
            new("The standard", "https://cai.canine.dev/spec"),
            new("Dimensions & lenses", "https://cai.canine.dev/dimensions"),
            new("Verify a survey", "https://cai.canine.dev/verify"),
            new("The registry", "https://cai.canine.dev/registry"),
            new("Reference scorer / CLI", "https://cai.canine.dev/cli"),
        ]),
        new("For buyers",
        [
            new("Assay — appraise software", "https://assay.canine.dev"),
            new("How Assay works", "https://assay.canine.dev/how-it-works"),
            new("Due-diligence dossier", "https://assay.canine.dev/reports/due-diligence"),
            new("Tender & delivery verification", "https://assay.canine.dev/reports/tender"),
        ]),
        new("Trust",
        [
            new("Security & data", "/security"),
            new("DPA", "/dpa"),
            new("Terms", "/tos"),
            new("About", "/about"),
            new("Contact", "/contact"),
        ]),
    ];

    // ── assay ─────────────────────────────────────────────────────────────────────
    private static readonly IReadOnlyList<NavEntry> AssayNav =
    [
        new("How it works", "/how-it-works"),
        new("Reports", Children:
        [
            new("All reports", "/reports", "The deliverables catalog"),
            new("Consequences report", "/reports/consequences", "What the findings mean for you"),
            new("Due-diligence dossier", "/reports/due-diligence", "Data-room DD, LOI to close"),
            new("Tender & delivery verification", "/reports/tender", "CAI ≥ 80 in the RFP, checked at delivery"),
            new("Contract appendix & attestation", "/reports/attestation", "Bind criteria into the deal"),
            new("Compliance & signing pack", "/reports/compliance", "Signed, audit-defensible conformance"),
            new("Portfolio appraisal", "/reports/portfolio", "A book of software assets, rolled up"),
        ]),
        new("Who it's for", Children:
        [
            new("Buyers & procurement", "/for-buyers", "Verify what you can't read"),
            new("Acquirers, investors & insurers", "/for-acquirers", "Appraise the asset"),
            new("Software owners", "/for-owners", "The condition of what you own"),
            new("Compliance & regulated", "/for-compliance", "Audit-defensible conformance"),
            new("Decision-makers", "/for-decision-makers", "Is the asset sound? What's the risk?"),
        ]),
        new("Pricing", "/pricing"),
    ];

    private static readonly HeaderAct AssayCta = new("Talk to us", "/contact");
    private static readonly HeaderAct? AssayQuiet = new("Sign in", "https://app.assay.canine.dev/");
    private const string AssayCopy = "© 2025–2026 · Know what the software is worth before you sign.";

    private static readonly IReadOnlyList<FooterCol> AssayFooter =
    [
        new("Reports",
        [
            new("Consequences report", "/reports/consequences"),
            new("Due-diligence dossier", "/reports/due-diligence"),
            new("Tender & delivery verification", "/reports/tender"),
            new("Contract appendix & attestation", "/reports/attestation"),
            new("Compliance & signing pack", "/reports/compliance"),
            new("Portfolio appraisal", "/reports/portfolio"),
        ]),
        new("Who it's for",
        [
            new("Buyers & procurement", "/for-buyers"),
            new("Acquirers & investors", "/for-acquirers"),
            new("Software owners", "/for-owners"),
            new("Compliance & regulated", "/for-compliance"),
            new("Decision-makers", "/for-decision-makers"),
        ]),
        new("How it works",
        [
            new("The evidence flow", "/how-it-works"),
            new("Compliance & audit", "/compliance"),
            new("Verify any number", "https://cai.canine.dev/verify"),
            new("The registry", "https://cai.canine.dev/registry"),
            new("The CAI standard", "https://cai.canine.dev"),
        ]),
        new("Producers",
        [
            new("Have the code? Get a scan — Watchdog", "https://watchdog.canine.dev"),
            new("Providers: prove a delivery", "/reports/attestation"),
        ]),
        new("Trust",
        [
            new("Security & data", "https://watchdog.canine.dev/security"),
            new("DPA", "https://watchdog.canine.dev/dpa"),
            new("Terms", "https://watchdog.canine.dev/tos"),
            new("About", "https://watchdog.canine.dev/about"),
            new("Talk to sales", "/contact"),
        ]),
    ];

    // ── cai ───────────────────────────────────────────────────────────────────────
    private static readonly IReadOnlyList<NavEntry> CaiNav =
    [
        new("The standard", "/spec"),
        new("Dimensions & lenses", "/dimensions"),
        new("Rubric versions", "/rubric"),
        new("Verify", "/verify"),
        new("Registry", "/registry"),
        new("Scorer / CLI", "/cli"),
    ];

    private static readonly HeaderAct CaiCta = new("Verify a survey", "/verify");
    private static readonly HeaderAct? CaiQuiet = new("Produce a survey ↗", "https://watchdog.canine.dev");
    private const string CaiCopy = "© 2025–2026 · The open standard for codebase assurance.";

    private static readonly IReadOnlyList<FooterCol> CaiFooter =
    [
        new("The standard",
        [
            new("How the CAI is computed", "/spec"),
            new("Dimensions & lenses", "/dimensions"),
            new("Rubric versions & governance", "/rubric"),
            new("Verify a survey", "/verify"),
            new("The registry", "/registry"),
            new("Reference scorer & CLI", "/cli"),
        ]),
        new("Producers",
        [
            new("Watchdog — survey a repo", "https://watchdog.canine.dev"),
            new("What Watchdog measures", "https://watchdog.canine.dev/methodology"),
            new("Agents & MCP", "https://watchdog.canine.dev/agents"),
        ]),
        new("Consumers",
        [
            new("Assay — commission a decision", "https://assay.canine.dev"),
            new("How Assay works", "https://assay.canine.dev/how-it-works"),
            new("The reports catalog", "https://assay.canine.dev/reports"),
        ]),
        new("Trust",
        [
            new("About Canine Development", "https://watchdog.canine.dev/about"),
            new("Security & data", "https://watchdog.canine.dev/security"),
            new("Contact", "https://watchdog.canine.dev/contact"),
        ]),
    ];

    // ── canine (the studio marketing site, www.canine.dev) ───────────────────────
    // Chrome lifted verbatim from the Blazor source (canine.dev
    // Components/Layout/MainLayout.razor), the same way the others mirror site.ts.
    // Transcription notes (the source is hand-written Razor, not CMS content):
    //  • The header/footer "/#what"-style in-page anchors keep their real deployed
    //    hrefs (origin + fragment); published sections carry no named anchors, so the
    //    fragment lands at the top of home — labels and destinations stay truthful.
    //  • The header "Contact" nav-cta pill is the header CTA; there is no quiet link.
    //  • The footer-about blurb ("Independent software-assurance studio · Denmark,
    //    since 2021.") has no slot in Imprint's footer model — the same line appears
    //    verbatim as the home hero kicker, so no copy is lost.
    //  • The copy line freezes the layout's dynamic © year at 2026 (migration time).
    private static readonly IReadOnlyList<NavEntry> CanineNav =
    [
        new("What we do", "/#what"),
        new("Products", "/#products"),
        new("How we work", "/#doctrine"),
        new("Team", "/#team"),
    ];

    private static readonly HeaderAct CanineCta = new("Contact", "/contact");
    private static readonly HeaderAct? CanineQuiet = null;
    private const string CanineCopy = "© 2026 Canine Development — assuring software quality since 2021.";

    private static readonly IReadOnlyList<FooterCol> CanineFooter =
    [
        new("Products",
        [
            new("Watchdog", "https://watchdog.canine.dev"),
            new("CAI standard", "https://cai.canine.dev"),
            new("Unfold", "https://unfold.canine.dev"),
        ]),
        new("Studio",
        [
            new("What we do", "/#what"),
            new("Team", "/#team"),
            new("Contact", "/contact"),
        ]),
    ];
}
