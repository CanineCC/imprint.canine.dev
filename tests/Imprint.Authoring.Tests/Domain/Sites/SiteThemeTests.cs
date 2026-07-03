using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Domain.Sites.Events;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Domain.Sites;

public sealed class SiteThemeTests
{
    private static readonly SiteId Id = SiteId.New();
    private static readonly Locale En = new("en");
    private static SiteCreated Created => new(Id, "Site", En);

    private static readonly Typography ValidTypography = new(
        Heading: FontStack.Serif,
        Body: FontStack.Humanist,
        BaseSizePx: 18,
        ScaleRatio: 1.333,
        RadiusPx: 12,
        Spacing: SpacingScale.Spacious);

    // -------------------------------------------------------------- SetThemeToken

    [Fact]
    public void SetThemeToken_known_token_with_valid_colors_raises_token_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetThemeToken("primary", "#ff0000", "oklch(70% 0.15 265)"))
            .ThenRaised(new SiteThemeTokenChanged("primary", "#ff0000", "oklch(70% 0.15 265)"));

    [Fact]
    public void SetThemeToken_updates_the_theme_state()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetThemeToken("accent", "#123456", "#654321"));

        Assert.Equal(new ThemeToken("#123456", "#654321"), outcome.Aggregate.Theme.Tokens.Get("accent"));
    }

    [Fact]
    public void SetThemeToken_unknown_token_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetThemeToken("brand-glow", "#ff0000", "#00ff00"))
            .ThenFails("not a theme token");

    [Fact]
    public void SetThemeToken_invalid_light_color_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetThemeToken("primary", "reddish", "#00ff00"))
            .ThenFails("not a valid CSS color");

    [Fact]
    public void SetThemeToken_invalid_dark_color_is_rejected() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetThemeToken("primary", "#ff0000", "url(javascript:alert(1))"))
            .ThenFails("not a valid CSS color");

    [Fact]
    public void SetThemeToken_with_unchanged_default_values_raises_nothing()
    {
        var primary = Theme.Default.Tokens.Get("primary")!;

        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetThemeToken("primary", primary.Light, primary.Dark))
            .ThenNothing();
    }

    [Fact]
    public void SetThemeToken_unchanged_after_a_previous_change_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteThemeTokenChanged("accent", "#123456", "#654321"))
            .When(s => s.SetThemeToken("accent", "#123456", "#654321"))
            .ThenNothing();

    // -------------------------------------------------------------- SetTypography

    [Fact]
    public void SetTypography_valid_typography_raises_typography_changed() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetTypography(ValidTypography))
            .ThenRaised(new SiteTypographyChanged(ValidTypography));

    [Fact]
    public void SetTypography_updates_the_theme_state()
    {
        var outcome = AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetTypography(ValidTypography));

        Assert.Equal(ValidTypography, outcome.Aggregate.Theme.Typography);
    }

    [Theory]
    [InlineData(13, 1.25, 8)]  // base size below minimum
    [InlineData(21, 1.25, 8)]  // base size above maximum
    [InlineData(16, 1.1, 8)]   // scale ratio below minimum
    [InlineData(16, 1.6, 8)]   // scale ratio above maximum
    [InlineData(16, 1.25, -1)] // negative radius
    [InlineData(16, 1.25, 25)] // radius above maximum
    public void SetTypography_out_of_range_values_are_rejected(int baseSizePx, double scaleRatio, int radiusPx) =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetTypography(ValidTypography with
            {
                BaseSizePx = baseSizePx,
                ScaleRatio = scaleRatio,
                RadiusPx = radiusPx,
            }))
            .ThenFails("out of range");

    [Fact]
    public void SetTypography_with_unchanged_typography_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created)
            .When(s => s.SetTypography(Theme.Default.Typography))
            .ThenNothing();

    [Fact]
    public void SetTypography_unchanged_after_a_previous_change_raises_nothing() =>
        AggregateSpec.For<Site>()
            .Given(Created, new SiteTypographyChanged(ValidTypography))
            .When(s => s.SetTypography(ValidTypography with { }))
            .ThenNothing();
}
