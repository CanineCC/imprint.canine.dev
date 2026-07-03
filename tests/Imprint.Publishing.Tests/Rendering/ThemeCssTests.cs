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
}
