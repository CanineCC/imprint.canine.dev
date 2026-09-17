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

    /// <summary>
    /// ★ The Panels kicker rule is decoration, and at kicker size it reads as a dash inside the label rather
    /// than as a mark beside it. It is off unless a site asks for it — and it is switched by <c>display</c>,
    /// not by <c>content</c> or <c>width</c>: the label is an inline-flex box with a 0.55rem gap, so a
    /// zero-width pseudo-element would still be a flex item and would leave that gap standing in front of
    /// every label on the page.
    /// </summary>
    [Fact]
    public void The_panel_kicker_rule_is_off_by_default_and_switched_by_display()
    {
        var off = ThemeCss.Emit(Theme.Default);
        Assert.Contains("--ip-kicker-rule-display: none", off);

        var on = ThemeCss.Emit(Theme.Default with
        {
            Typography = Theme.Default.Typography with { PanelKickerRule = true },
        });
        Assert.Contains("--ip-kicker-rule-display: inline-block", on);

        // The sheet must consume the variable on display — never re-hard-code the old value.
        var css = WithoutComments(ThemeCss.MarketingCss);
        Assert.Contains("display: var(--ip-kicker-rule-display, none)", css);
        Assert.DoesNotContain(
            "content: \"\"; width: 18px; height: 1px; background: var(--accent); display: inline-block;",
            css);
    }

    /// <summary>
    /// Every eyebrow on the site is accent-coloured, Panels included. Panels used to override the label to
    /// <c>--muted</c>, which read as deliberate only while the accent rule sat in front of it carrying the
    /// colour; once that rule was switched off by default it left the panel eyebrow the single eyebrow on the
    /// estate with no accent on it at all.
    /// </summary>
    [Fact]
    public void A_panel_kicker_takes_the_accent_colour_like_every_other_kicker()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        // The base rule is where the colour comes from, for panels too.
        Assert.Contains("text-transform: uppercase; color: var(--accent);", css);

        // The Panels override must not reintroduce a colour of its own.
        var panelRule = css[css.IndexOf(".ip-ap-panels .ip-prose.ip-kicker > p > strong {", StringComparison.Ordinal)..];
        panelRule = panelRule[..panelRule.IndexOf('}')];
        Assert.DoesNotContain("color:", panelRule);
    }

    /// <summary>
    /// ★ A hero with ONE paragraph has no trailing paragraph — it has a lede. Keyed on position alone the
    /// closing-line rule caught it anyway and set the page's only intro at fine-print size (/badge read as a
    /// footnote with no lede at all). The demotion now requires a prose sibling ahead of it.
    /// </summary>
    [Fact]
    public void A_heros_only_paragraph_is_never_demoted_to_a_closing_line()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        Assert.Contains(
            ".ip-ap-hero > .ip-stack > .ip-prose:not(.ip-kicker) ~ .ip-prose:last-of-type",
            css);

        // The unguarded form is what produced the bug: it must not come back.
        Assert.DoesNotContain(
            ".ip-ap-hero > .ip-stack > .ip-prose:last-of-type {",
            css);

        // ★ And the guard only bites if the generic trailing rule stays out of heroes. That selector is
        // the more specific of the two, so while it also matched a hero it won outright and the guard
        // above was dead letter — the lone lede went on rendering at --fs-xs with nothing to show why.
        var generic = css.Split('\n').Single(l =>
            l.Contains("[class*=\"ip-ap-\"]", StringComparison.Ordinal) &&
            l.Contains(".ip-prose:last-child:not(.ip-kicker):not(:has(ul))", StringComparison.Ordinal));
        Assert.Contains(":not(.ip-ap-hero)", generic);
    }

    /// <summary>
    /// ★ The reading measure and the section band are ONE decision. They were made separately — the band
    /// sized so three cards fit a row, the measure sized so a line stays readable — and nobody compared
    /// them, leaving text at 47% of the band it sat in: a paragraph looking like half a page next to a card
    /// grid that filled it. A readable line cannot grow past ~640px, so the band has to come down to meet
    /// the measure rather than the other way round.
    /// </summary>
    [Fact]
    public void The_reading_measure_is_a_fair_share_of_the_section_band()
    {
        var band = Rem(ThemeCss.StructuralCss, @"calc\(50% - ([\d.]+)rem\)") * 2;
        var measure = Rem(ThemeCss.MarketingCss, @"--mk-measure: ([\d.]+)rem;");

        var share = measure / band;
        Assert.True(
            share >= 0.55,
            $"text is {share:P0} of the {band}rem band — a paragraph should not read as half a page beside a card grid.");

        // ★ And the pair has to stay READABLE, which is a character count, not a width. A wider column is
        // only legitimate if the text in it is bigger: an average glyph is about half the font size, so
        // characters-per-line is the measure divided by half the body size. Past ~80 the eye loses the
        // line on the way back to the left, whatever the band is doing.
        var bodyPx = double.Parse(
            System.Text.RegularExpressions.Regex.Match(ThemeCss.MarketingCss, @"--mk-body: (\d+)px;").Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);
        var charsPerLine = (measure * 16) / (bodyPx / 2);
        Assert.True(
            charsPerLine <= 80,
            $"{measure}rem of {bodyPx}px text is ~{charsPerLine:N0} characters a line; widen the text only by making it bigger.");
    }

    private static double Rem(string css, string pattern)
    {
        var m = System.Text.RegularExpressions.Regex.Match(css, pattern);
        Assert.True(m.Success, $"no match for {pattern}");
        return double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// ★ The content column is CENTRED; the text inside it is left-aligned. Those are two different
    /// things and conflating them is what sent every section hard against the left edge with the right
    /// half empty. Centring a block is fine. Centring text costs the reader the fixed left edge the eye
    /// returns to on every line.
    /// </summary>
    [Fact]
    public void The_content_column_is_centred_and_its_text_is_left_aligned()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        var start = css.IndexOf(".ip-ap-contact > .ip-stack > .ip-prose", StringComparison.Ordinal);
        Assert.True(start >= 0, "the section body-text rule is gone");
        var rule = css[start..(css.IndexOf('}', start) + 1)];

        Assert.Contains("text-align: left", rule);
        Assert.Contains("margin-inline: auto", rule);
        Assert.Contains("max-width: var(--mk-measure)", rule);

        var stackStart = css.IndexOf(".ip-ap-contact > .ip-stack {", StringComparison.Ordinal);
        Assert.True(stackStart >= 0, "the section stack rule is gone");
        var stackRule = css[stackStart..(css.IndexOf('}', stackStart) + 1)];
        Assert.Contains("align-items: center", stackRule);
    }

    /// <summary>
    /// ★ A header is NEVER narrower than the content under it -- that is the whole rule. It takes the
    /// headline measure, which is deliberately wider than the copy measure, and stretches to the band
    /// when the section holds a grid or a widget so it matches its own cards. Full width everywhere was
    /// wrong for the opposite reason: at 33px a heading holds ~68 characters on one line and 27 of CAI's
    /// 31 headings fit on one, which reads as a banner rather than a headline.
    /// </summary>
    [Fact]
    public void A_header_is_never_narrower_than_the_content_beneath_it()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        var head = Rem(css, @"--mk-measure-head: ([\d.]+)rem;");
        var copy = Rem(css, @"--mk-measure: ([\d.]+)rem;");
        Assert.True(head > copy, $"the headline measure ({head}rem) must exceed the copy measure ({copy}rem)");

        // ...and it is a HEADLINE measure, not the band: display type past ~45 characters stops reading
        // as a headline. 46rem at the largest heading size is about 43.
        Assert.True(head <= 52, $"{head}rem of display type is a banner, not a headline");

        // With a grid, columns or a widget present it matches them instead of staying capped.
        Assert.Contains(
            "[class*=\"ip-ap-\"] > .ip-stack:has(> :is(.ip-grid, .ip-columns, .ip-widget)) > :is(h2, h3)",
            css);

        // The hero and CTA are centred statements and keep their own alignment.
        var align = css.IndexOf("> .ip-stack > :is(h2, h3) { text-align: left; }", StringComparison.Ordinal);
        Assert.True(align >= 0, "the header alignment rule is gone");
        var lineStart = css.LastIndexOf('\n', align) + 1;
        var rule = css[lineStart..align];
        Assert.Contains(":not(.ip-ap-hero)", rule);
        Assert.Contains(":not(.ip-ap-cta)", rule);
    }

    /// <summary>
    /// <summary>
    /// ★ The lede size is a contrast, so it is only spent where there is something to contrast with. Most CAI
    /// heroes are kicker / headline / one paragraph / buttons — and that lone paragraph was being set larger
    /// than every other paragraph on its page with nothing smaller beside it to justify the jump, which reads
    /// as an arbitrary size rather than an intro. It takes the lede size only while a paragraph follows it.
    /// </summary>
    [Fact]
    public void The_hero_lede_size_applies_only_while_a_second_paragraph_follows()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        var lede = css.Split('\n').Single(l =>
            l.Contains(".ip-ap-hero > .ip-stack > .ip-prose:not(.ip-kicker):not(.ip-prose-secondary)", StringComparison.Ordinal));

        Assert.Contains(":has(~ .ip-prose:not(.ip-kicker))", lede);
    }

    /// <summary>
    /// Emphasis is a statement about what a paragraph IS, so every positional rule that could shrink it has
    /// to stand aside for it — otherwise the class is set, reads as applied, and is silently outranked.
    /// </summary>
    [Fact]
    public void An_explicit_emphasis_is_never_overridden_by_a_positional_rule()
    {
        var css = WithoutComments(ThemeCss.MarketingCss);

        Assert.Contains("[class*=\"ip-ap-\"] .ip-prose.ip-prose-secondary", css);
        Assert.Contains("[class*=\"ip-ap-\"] .ip-prose.ip-prose-fine", css);

        // ★ And every one of them must also require something visual before the paragraph. This size is
        // for a CAPTION — it sits under a button row, a grid, a table, a figure. Keyed on "last in the
        // section" alone it also caught a section's own body text whenever nothing followed it, so two
        // sections written identically printed at different sizes depending on whether one ended with
        // buttons. Straight after a heading, a paragraph is body text.
        foreach (var line in css.Split('\n'))
        {
            if (!line.Contains(".ip-prose:last-child", StringComparison.Ordinal)) { continue; }

            Assert.True(
                line.Contains(":not(:is(h1, h2, h3, h4, .ip-prose)) +", StringComparison.Ordinal),
                $"A trailing-paragraph rule can still shrink a section's body text: {line.Trim()}");
        }

        // Every rule that demotes a trailing paragraph must exclude an explicit Secondary.
        foreach (var line in css.Split('\n'))
        {
            var isTrailingRule =
                line.Contains(".ip-prose:last-child:not(.ip-kicker)", StringComparison.Ordinal) ||
                line.Contains(".ip-prose:last-of-type", StringComparison.Ordinal);

            if (!isTrailingRule) { continue; }

            Assert.True(
                line.Contains(":not(.ip-prose-secondary)", StringComparison.Ordinal),
                $"A positional rule can still outrank an explicit emphasis: {line.Trim()}");
        }
    }

    /// <summary>CSS with comments removed, so an assertion about the RULES cannot be satisfied — or defeated —
    /// by the prose explaining them.</summary>
    private static string WithoutComments(string css) =>
        System.Text.RegularExpressions.Regex.Replace(css, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
}
