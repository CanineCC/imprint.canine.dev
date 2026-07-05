using Imprint.Authoring.Domain.Sites;

namespace ContentSeeder;

/// <summary>
/// The per-site design theme, computed VERBATIM from the marketing source of truth
/// (cms.canine.dev/packages/ui/styles/canine.css): the shared graphite/paper neutral
/// family (identical across the three brands) plus each brand's own accent ramp
/// (canine's per-<c>data-brand</c> accent/-ink/-wash/-strong/-on values), in imprint's
/// light-then-dark token shape. Typography mirrors canine too — Schibsted Grotesk for the
/// UI/headings (self-hosted), the system sans for body, JetBrains Mono for code; the 14px
/// base with a 1.25 scale and a 10px radius match canine's <c>--fs-*</c> / <c>--r-md</c>.
///
/// The seeder applies these through the existing ChangeThemeToken / ChangeTypography
/// commands, so nothing here is a new mechanism — only faithfully transcribed values.
/// </summary>
public static class Themes
{
    /// <summary>One theme token's light and dark value (canine :root vs [data-theme=light]).</summary>
    public sealed record Tok(string Light, string Dark);

    // ── Shared neutrals — canine :root (dark "graphite") + [data-theme=light] ("paper").
    //    The family default for watchdog + cai. ────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, Tok> Neutrals = new Dictionary<string, Tok>
    {
        ["background"] = new("#fcfcfd", "#15191e"),  // --bg
        ["surface"] = new("#f5f7f9", "#1c2127"),     // --surface
        ["surface-alt"] = new("#edf0f3", "#232a31"), // --surface-2
        ["text"] = new("#1c2126", "#e4e9ed"),        // --ink
        ["text-muted"] = new("#616b76", "#8694a1"),  // --muted
        ["border"] = new("#e1e6eb", "#2d353e"),      // --border
    };

    // ── Assay "Dal" neutrals — the warm-paper family (docs/design/dal-assay.md §3.1
    //    light "paper" + §3.2 dark "slate ledger", transcribed to imprint's light-then-
    //    dark token shape). Warm ivory + charcoal, deliberately NOT the cool graphite
    //    above — the warmth is the first thing that reads as "not Watchdog." Light is the
    //    canonical Assay face (§1.4); the dark values serve OS-dark viewers only. ────────
    public static readonly IReadOnlyDictionary<string, Tok> AssayPaper = new Dictionary<string, Tok>
    {
        ["background"] = new("#fbfaf7", "#1b1a17"),  // --bg (warm ivory)
        ["surface"] = new("#f5f2ec", "#232120"),     // --surface (vellum)
        ["surface-alt"] = new("#ece7de", "#2c2a27"), // --surface-2
        ["text"] = new("#22201c", "#ece7dd"),        // --ink (warm charcoal)
        ["text-muted"] = new("#6b6459", "#9a9284"),  // --muted
        ["border"] = new("#e4ded2", "#38352f"),      // --border
    };

    // ── Per-brand accent ramp — canine :root[data-brand=…] (dark) +
    //    :root[data-brand=…][data-theme=light] (light). Mapped to imprint's
    //    primary / primary-ink / primary-wash / primary-strong / on-primary. ────────────
    public sealed record AccentRamp(Tok Primary, Tok Ink, Tok Wash, Tok Strong, Tok OnPrimary);

    // watchdog "steel" (the family default in canine :root)
    public static readonly AccentRamp Watchdog = new(
        Primary: new("#4682b4", "#7faace"),
        Ink: new("#2f5d85", "#9bbedb"),
        Wash: new("#eaf1f7", "#1e2c39"),
        Strong: new("#264b6b", "#b7d2e8"),
        OnPrimary: new("#ffffff", "#15191e"));

    // assay "Dal" copper (docs/design/dal-assay.md §3.3, the metallurgical accent). Bare
    //    --accent/primary is UI/large-text/fill only; primary-ink (--accent-ink) is the
    //    link + body-size accent that clears AA on ivory; primary-strong (--accent-strong)
    //    fills the CTAs. Light then dark, transcribed verbatim from the §12 token sheet.
    public static readonly AccentRamp Assay = new(
        Primary: new("#9a6a3c", "#c99a6a"),
        Ink: new("#6f4a26", "#d9b48c"),
        Wash: new("#f3eadf", "#2e2115"),
        Strong: new("#5a3a1e", "#e4c9a8"),
        OnPrimary: new("#fbfaf7", "#1b1a17"));

    // cai (harmonized teal sibling)
    public static readonly AccentRamp Cai = new(
        Primary: new("#2e7d64", "#6fbfa4"),
        Ink: new("#226050", "#8fcdb8"),
        Wash: new("#e6f1ec", "#1b332c"),
        Strong: new("#1c4f41", "#aedccb"),
        OnPrimary: new("#ffffff", "#15191e"));

    // ── Typography — canine's UI/mono choices + scale. Schibsted Grotesk headings,
    //    system-sans body, 14px base × 1.25 scale, 10px radius (canine --r-md). ──────────
    public static readonly Typography Marketing = new(
        Heading: FontStack.Grotesk,
        Body: FontStack.Sans,
        BaseSizePx: 14,
        ScaleRatio: 1.25,
        RadiusPx: 10,
        Spacing: SpacingScale.Comfortable);

    // ── Assay "Dal" typography — the editorial serif voice (dal-assay.md §4). The
    //    marketing layer drives every surface off --ip-font-heading, so choosing the Serif
    //    stack gives Assay its "a memo, not a dashboard" register (Charter → … → Georgia
    //    serif; no Spectral binary ships, and §4/§12 sanction the Georgia serif fallback).
    //    Squarer radius (§5: 8px, the --r-md ledger feel) vs. the shared 10px. ────────────
    public static readonly Typography Editorial = new(
        Heading: FontStack.Serif,
        Body: FontStack.Sans,
        BaseSizePx: 14,
        ScaleRatio: 1.25,
        RadiusPx: 8,
        Spacing: SpacingScale.Comfortable);

    /// <summary>Every (token, light, dark) triple for a brand: its neutral family + accent ramp.</summary>
    public static IEnumerable<(string Token, string Light, string Dark)> TokensFor(
        IReadOnlyDictionary<string, Tok> neutrals, AccentRamp accent)
    {
        foreach (var (name, tok) in neutrals)
        {
            yield return (name, tok.Light, tok.Dark);
        }

        yield return ("primary", accent.Primary.Light, accent.Primary.Dark);
        yield return ("primary-ink", accent.Ink.Light, accent.Ink.Dark);
        yield return ("primary-wash", accent.Wash.Light, accent.Wash.Dark);
        yield return ("primary-strong", accent.Strong.Light, accent.Strong.Dark);
        yield return ("on-primary", accent.OnPrimary.Light, accent.OnPrimary.Dark);
        // `accent` (imprint's ninth token) isn't used by the marketing layer — the whole
        // marketing accent maps to `primary*` — so it's left at the theme default.
    }
}
