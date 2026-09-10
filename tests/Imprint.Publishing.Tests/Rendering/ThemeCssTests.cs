using Imprint.Authoring.Domain.Sites;
using Imprint.Rendering;

namespace Imprint.Publishing.Tests.Rendering;

public sealed class ThemeCssTests
{
    [Fact]
    public void Emit_is_deterministic_for_structurally_equal_themes()
    {
        var rebuilt = new Theme(
            TokenSet.Of(Theme.Default.Tokens.ToDictionary(t => t.Key, t => t.Value)),
            Theme.Default.Typography with { });

        Assert.Equal(ThemeCss.Emit(Theme.Default), ThemeCss.Emit(rebuilt));
    }

    [Fact]
    public void Every_token_emits_a_light_dark_custom_property()
    {
        var css = ThemeCss.Emit(Theme.Default);

        foreach (var token in ThemeTokens.All)
        {
            Assert.Contains($"--ip-{token}: light-dark(", css);
        }
    }

    [Fact]
    public void Token_values_appear_in_light_dark_order()
    {
        var css = ThemeCss.Emit(Theme.Default);

        Assert.Contains("--ip-background: light-dark(#ffffff, #0f1115);", css);
        Assert.Contains("--ip-primary: light-dark(#3b5bdb, #748ffc);", css);
    }

    [Fact]
    public void Root_block_declares_color_scheme_and_explicit_overrides()
    {
        var css = ThemeCss.Emit(Theme.Default);

        Assert.StartsWith(":root {", css);
        Assert.Contains("color-scheme: light dark;", css);
        Assert.Contains(":root[data-theme=light] { color-scheme: light; }", css);
        Assert.Contains(":root[data-theme=dark] { color-scheme: dark; }", css);
    }

    [Fact]
    public void EmitScoped_wraps_the_same_variables_under_the_selector()
    {
        var scoped = ThemeCss.EmitScoped(Theme.Default, ".ed-canvas");

        Assert.Equal(ThemeCss.Emit(Theme.Default).Replace(":root", ".ed-canvas"), scoped);
        Assert.StartsWith(".ed-canvas {", scoped);
        Assert.Contains(".ed-canvas[data-theme=dark] { color-scheme: dark; }", scoped);
        Assert.DoesNotContain(":root", scoped);
    }

    [Fact]
    public void Fluid_type_scale_grows_from_base_size_by_the_ratio()
    {
        var css = ThemeCss.Emit(Theme.Default);

        // Base 16px growing ~12% over the 320→1280 viewport span.
        Assert.Contains("--ip-text-base: clamp(1rem, 0.96rem + 0.2vw, 1.12rem);", css);
        // One ratio step up: 16 × 1.25 = 20px.
        Assert.Contains("--ip-text-lg: clamp(1.25rem, 1.2rem + 0.25vw, 1.4rem);", css);
        foreach (var step in (string[])["sm", "base", "lg", "xl", "2xl", "3xl", "4xl"])
        {
            Assert.Contains($"--ip-text-{step}: clamp(", css);
        }
    }

    [Fact]
    public void Radius_and_comfortable_spacing_emit_expected_values()
    {
        var css = ThemeCss.Emit(Theme.Default);

        Assert.Contains("--ip-radius: 8px;", css);
        Assert.Contains("--ip-space-1: 0.25rem;", css);
        Assert.Contains("--ip-space-4: 1rem;", css);
        Assert.Contains("--ip-space-8: 2rem;", css);
    }

    [Theory]
    [InlineData(SpacingScale.Compact, "--ip-space-4: 0.8rem;")]
    [InlineData(SpacingScale.Comfortable, "--ip-space-4: 1rem;")]
    [InlineData(SpacingScale.Spacious, "--ip-space-4: 1.25rem;")]
    public void Spacing_scale_multiplies_the_spacing_variables(SpacingScale scale, string expected)
    {
        var theme = Theme.Default with { Typography = Theme.Default.Typography with { Spacing = scale } };

        Assert.Contains(expected, ThemeCss.Emit(theme));
    }

    [Fact]
    public void Font_stacks_are_real_system_stacks_per_choice()
    {
        var theme = Theme.Default with
        {
            Typography = Theme.Default.Typography with { Heading = FontStack.Geometric, Body = FontStack.Serif },
        };

        var css = ThemeCss.Emit(theme);

        Assert.Contains("--ip-font-heading: 'Avenir Next', Avenir,", css);
        Assert.Contains("--ip-font-body: Charter,", css);
        Assert.Contains("sans-serif;", css);
    }

    [Fact]
    public void Structural_css_loads_and_contains_the_layout_system()
    {
        var css = ThemeCss.StructuralCss;

        Assert.Contains(".ip-section", css);
        Assert.Contains("container-type: inline-size", css);
        Assert.Contains(".ip-stack", css);
        Assert.Contains(".ip-columns", css);
        Assert.Contains(".ip-grid", css);
        Assert.Contains(".ip-prose", css);
        Assert.Contains(".ip-btn", css);
        Assert.Contains("@container (max-width: 480px)", css);
        Assert.Contains("@container (max-width: 640px)", css);
        Assert.Contains("@container (max-width: 768px)", css);
        Assert.Contains("prefers-reduced-motion", css);
        Assert.Contains(":focus-visible", css);
    }

    [Fact]
    public void Structural_css_stays_within_the_size_budget()
    {
        // It ships on every published page — 8 KB raw is the agreed ceiling.
        Assert.True(
            ThemeCss.StructuralCss.Length <= 8 * 1024,
            $"imprint-base.css is {ThemeCss.StructuralCss.Length} bytes; the budget is 8192.");
    }

    /// <summary>
    /// ★ The page IS the printable artefact. The guides' separate PDF pipeline was deleted on 2026-09-06
    /// because the manuscript it rendered from drifted behind the page — one guide's download still named
    /// dimensions retired the day before. Removing a second source is only defensible while the remaining one
    /// prints, so these rules are load-bearing for that decision, not cosmetic.
    /// </summary>
    [Fact]
    public void Print_css_loads_and_carries_the_rules_the_pdf_removal_depends_on()
    {
        var css = ThemeCss.PrintCss;

        Assert.Contains("@media print", css);

        // Structure: paper has no horizontal room to spend on a sidebar track.
        Assert.Contains(".ip-columns, .ip-grid { grid-template-columns: 1fr !important; }", css);

        // ★ The guide sidebar itself. Collapsing the grid ALONE made printing worse — the sidebar then
        // printed full-width above the article — which only showed up by printing the page and looking.
        Assert.Contains(".ip-ap-doc .ip-columns > :first-child", css);

        // A printed link whose destination is invisible is a dead end, and the guides lean on citations.
        Assert.Contains("a[href^=\"http\"]::after", css);

        // `overflow-wrap: anywhere`, never `word-break: break-all`: break-all is greedy and split
        // "https://watchdog.canine.dev" mid-domain as "https://watch / dog.canine.dev" — a typo in a document
        // someone may act on. Asserted against the DECLARATIONS, not the prose: the sheet's comment names the
        // rejected property to explain why it is rejected, and a naive substring check reads that explanation
        // as the defect it warns about.
        Assert.Contains("overflow-wrap: anywhere", WithoutComments(css));
        Assert.DoesNotContain("word-break: break-all", WithoutComments(css));

        // Chrome that exists only to move a reader elsewhere.
        Assert.Contains(".ip-nav", css);
        Assert.Contains(".ip-btn", css);
    }

    /// <summary>
    /// Print is a MEDIUM, not a layer. Keeping these rules inside the structural sheet is what pushed
    /// imprint-base.css from 8,085 to 11,427 bytes and turned the deploy red; keeping them in their own sheet
    /// is what lets the structural budget go on meaning what it says.
    /// </summary>
    [Fact]
    public void Print_rules_live_in_the_print_sheet_and_nowhere_else()
    {
        Assert.DoesNotContain("@media print", ThemeCss.StructuralCss);
        Assert.DoesNotContain("@media print", ThemeCss.MarketingCss);
    }

    /// <summary>It ships on every published page too — so it gets a ceiling of its own rather than growing
    /// unwatched in the gap left by the structural budget.</summary>
    [Fact]
    public void Print_css_stays_within_its_own_size_budget()
    {
        Assert.True(
            ThemeCss.PrintCss.Length <= 8 * 1024,
            $"imprint-print.css is {ThemeCss.PrintCss.Length} bytes; the budget is 8192.");
    }

    /// <summary>
    /// ★ A rule that cannot fire is worse than a missing one, because it reads as covered. The print sheet hid
    /// <c>.ip-footer-fine</c> and then set its child <c>.ip-footer-copy</c> to <c>display: block !important</c>
    /// to keep the copyright line on paper — but <c>display</c> on a child cannot reveal it through a
    /// <c>display: none</c> ancestor, so the line had never once printed. The container must stay displayed and
    /// its OTHER children be hidden instead.
    /// </summary>
    [Fact]
    public void The_footer_copy_line_is_not_hidden_by_its_own_ancestor()
    {
        var css = WithoutComments(ThemeCss.PrintCss);

        Assert.Contains(".ip-footer-fine { display: block !important; }", css);
        Assert.Contains(".ip-footer-fine > :not(.ip-footer-copy) { display: none !important; }", css);

        // The container must not appear in the blanket hide list that caused the bug.
        var hideList = css[css.IndexOf(".ip-nav,", StringComparison.Ordinal)..];
        hideList = hideList[..hideList.IndexOf('}')];
        Assert.DoesNotContain(".ip-footer-fine", hideList);
    }

    /// <summary>An un-hydrated island prints as a box containing its own name. <c>:not(:defined)</c> is that
    /// state; the classes the rule used to name never existed in the published markup.</summary>
    [Fact]
    public void An_unhydrated_widget_island_does_not_print()
    {
        var css = WithoutComments(ThemeCss.PrintCss);

        Assert.Contains(".ip-widget:not(:defined)", css);
        Assert.DoesNotContain(".ip-widget-placeholder", css);
    }

    /// <summary>
    /// ★ The geometry proof for this rule lives in <c>Imprint.E2E.NarrowViewportTests</c>, which measures a real
    /// published page in a real 400px Chromium — and the deploy filter excludes Imprint.E2E, so in CI nothing
    /// would notice the rule going missing. This fact is the CI-visible half: it cannot prove the page fits, but
    /// it can prove the declaration that makes it fit is still in the sheet that ships.
    ///
    /// <para>Asserted against the DECLARATIONS, not the prose: the sheet's comment names both rejected
    /// properties in order to explain why they are rejected, and a naive substring check reads that explanation
    /// as the defect it warns about.</para>
    /// </summary>
    [Fact]
    public void Long_identifiers_fold_rather_than_widen_the_page()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        // Headings are the reported case; prose and body cells are the same class of content.
        Assert.Contains("h1, h2, h3, h4, .ip-prose p, .ip-prose li, .ip-prose a, .ip-table td", css);
        Assert.Contains("overflow-wrap: anywhere", css);

        // break-word wraps identically to the eye but leaves min-content alone, so a grid or table
        // track keeps stretching to hold the token; break-all breaks words that never needed it.
        Assert.DoesNotContain("overflow-wrap: break-word", css);
        Assert.DoesNotContain("word-break: break-all", css);
        Assert.DoesNotContain("word-break: break-word", css);

        // A column header is a one-word label. Folding it removed the floor the auto table layout
        // used to reserve that column, and "Version" printed as "VERSIO / N".
        Assert.DoesNotContain(".ip-table th, .ip-table td", css);
    }

    /// <summary>CSS with comments removed, so an assertion about the RULES cannot be satisfied — or defeated —
    /// by the prose explaining them.</summary>
    private static string WithoutComments(string css) =>
        System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
}
