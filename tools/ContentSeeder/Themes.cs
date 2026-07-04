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
    //    Identical for all three products. ─────────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, Tok> Neutrals = new Dictionary<string, Tok>
    {
        ["background"] = new("#fcfcfd", "#15191e"),  // --bg
        ["surface"] = new("#f5f7f9", "#1c2127"),     // --surface
        ["surface-alt"] = new("#edf0f3", "#232a31"), // --surface-2
        ["text"] = new("#1c2126", "#e4e9ed"),        // --ink
        ["text-muted"] = new("#616b76", "#8694a1"),  // --muted
        ["border"] = new("#e1e6eb", "#2d353e"),      // --border
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

    // assay (harmonized indigo sibling)
    public static readonly AccentRamp Assay = new(
        Primary: new("#4a5d96", "#8fa2d4"),
        Ink: new("#35456f", "#a9b8de"),
        Wash: new("#eceff7", "#232a44"),
        Strong: new("#2c3a61", "#c2cdea"),
        OnPrimary: new("#ffffff", "#15191e"));

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

    /// <summary>Every (token, light, dark) triple for a brand: the shared neutrals + the brand's accent ramp.</summary>
    public static IEnumerable<(string Token, string Light, string Dark)> TokensFor(AccentRamp accent)
    {
        foreach (var (name, tok) in Neutrals)
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
