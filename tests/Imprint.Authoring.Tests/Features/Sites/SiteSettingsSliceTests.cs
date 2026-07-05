using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.AddLocale;
using Imprint.Authoring.Features.Sites.ChangeDefaultLocale;
using Imprint.Authoring.Features.Sites.ChangeThemeToken;
using Imprint.Authoring.Features.Sites.ChangeTypography;
using Imprint.Authoring.Features.Sites.ConfigureEnvironments;
using Imprint.Authoring.Features.Sites.RemoveLocale;
using Imprint.Authoring.Features.Sites.RenameSite;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Sites;

/// <summary>The thin pass-through site slices: happy path, guards, read-model effect.</summary>
public sealed class SiteSettingsSliceTests
{
    // ---------------------------------------------------------------- RenameSite

    [Fact]
    public async Task RenameSite_happy_path_updates_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        await host.Ok(new RenameSite(siteId, "Rebranded"));

        Assert.Equal("Rebranded", host.Get<SiteOverview>().Current!.Name);
    }

    [Fact]
    public async Task RenameSite_with_empty_name_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new RenameSite(siteId, "  "));

        Assert.Contains("name cannot be empty", error);
        Assert.Equal("Test site", host.Get<SiteOverview>().Current!.Name);
    }

    // ----------------------------------------------------------------- AddLocale

    [Fact]
    public async Task AddLocale_happy_path_updates_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        await host.Ok(new AddLocale(siteId, "da-dk"));

        // The locale is normalized on parse: 'da-dk' → 'da-DK'.
        Assert.Equal([new Locale("en"), new Locale("da-DK")], host.Get<SiteOverview>().Current!.Locales);
    }

    [Fact]
    public async Task AddLocale_with_invalid_tag_fails_validation()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new AddLocale(siteId, "not a locale"));

        Assert.Contains("not a valid locale tag", error);
    }

    [Fact]
    public async Task AddLocale_that_is_already_present_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new AddLocale(siteId, "en"));

        Assert.Contains("already on this site", error);
    }

    // -------------------------------------------------------------- RemoveLocale

    [Fact]
    public async Task RemoveLocale_happy_path_updates_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        await host.Ok(new AddLocale(siteId, "da"));

        await host.Ok(new RemoveLocale(siteId, "da"));

        Assert.Equal([new Locale("en")], host.Get<SiteOverview>().Current!.Locales);
    }

    [Fact]
    public async Task RemoveLocale_of_the_default_locale_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new RemoveLocale(siteId, "en"));

        Assert.Contains("default locale", error);
    }

    [Fact]
    public async Task RemoveLocale_that_is_not_on_the_site_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new RemoveLocale(siteId, "fr"));

        Assert.Contains("not on this site", error);
    }

    // ------------------------------------------------------- ChangeDefaultLocale

    [Fact]
    public async Task ChangeDefaultLocale_happy_path_updates_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        await host.Ok(new AddLocale(siteId, "da"));

        await host.Ok(new ChangeDefaultLocale(siteId, "da"));

        Assert.Equal(new Locale("da"), host.Get<SiteOverview>().Current!.DefaultLocale);
    }

    [Fact]
    public async Task ChangeDefaultLocale_to_a_locale_not_on_the_site_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ChangeDefaultLocale(siteId, "fr"));

        Assert.Contains("not one of this site's locales", error);
    }

    [Fact]
    public async Task ChangeDefaultLocale_with_invalid_tag_fails_validation()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ChangeDefaultLocale(siteId, "!!"));

        Assert.Contains("not a valid locale tag", error);
    }

    // ------------------------------------------------------------ ChangeThemeToken

    [Fact]
    public async Task ChangeThemeToken_happy_path_updates_the_theme_in_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        await host.Ok(new ChangeThemeToken(siteId, "primary", "#123456", "#abcdef"));

        var theme = host.Get<SiteOverview>().Current!.Theme;
        Assert.Equal(new ThemeToken("#123456", "#abcdef"), theme.Tokens.Get("primary"));
    }

    [Fact]
    public async Task ChangeThemeToken_with_unknown_token_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ChangeThemeToken(siteId, "brand", "#123456", "#abcdef"));

        Assert.Contains("not a theme token", error);
    }

    [Fact]
    public async Task ChangeThemeToken_with_invalid_color_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ChangeThemeToken(siteId, "primary", "reddish", "#abcdef"));

        Assert.Contains("not a valid CSS color", error);
    }

    // ----------------------------------------------------------- ChangeTypography

    [Fact]
    public async Task ChangeTypography_happy_path_updates_the_theme_in_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var typography = new Typography(
            Heading: FontStack.Serif,
            Body: FontStack.Humanist,
            BaseSizePx: 18,
            ScaleRatio: 1.333,
            RadiusPx: 12,
            Spacing: SpacingScale.Spacious);

        await host.Ok(new ChangeTypography(siteId, typography));

        Assert.Equal(typography, host.Get<SiteOverview>().Current!.Theme.Typography);
    }

    [Fact]
    public async Task ChangeTypography_out_of_range_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var tooSmall = new Typography(FontStack.Sans, FontStack.Sans, BaseSizePx: 10, ScaleRatio: 1.25, RadiusPx: 8, Spacing: SpacingScale.Comfortable);

        var error = await host.Fails(new ChangeTypography(siteId, tooSmall));

        Assert.Contains("out of range", error);
    }

    // ------------------------------------------------------ ConfigureEnvironments

    [Fact]
    public async Task ConfigureEnvironments_happy_path_updates_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var environments = new DeployEnvironment[]
        {
            new("Test", "/var/www/test"),
            new("Production", "/var/www/prod", "https://acme.example"),
        };

        await host.Ok(new ConfigureEnvironments(siteId, environments));

        Assert.Equal(environments, host.Get<SiteOverview>().Get(siteId)!.Environments);
    }

    [Fact]
    public async Task ConfigureEnvironments_with_an_invalid_site_address_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ConfigureEnvironments(
            siteId, [new DeployEnvironment("Test", "/var/www/test", "not a url")]));

        Assert.Contains("absolute http(s) URL", error);
        Assert.Empty(host.Get<SiteOverview>().Get(siteId)!.Environments);
    }

    [Fact]
    public async Task ConfigureEnvironments_with_duplicate_names_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ConfigureEnvironments(
            siteId, [new DeployEnvironment("Prod", "/a"), new DeployEnvironment("prod", "/b")]));

        Assert.Contains("unique", error);
        Assert.Empty(host.Get<SiteOverview>().Get(siteId)!.Environments);
    }

    [Fact]
    public async Task ConfigureEnvironments_with_missing_folder_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new ConfigureEnvironments(
            siteId, [new DeployEnvironment("Test", "  ")]));

        Assert.Contains("publish folder", error);
    }
}
