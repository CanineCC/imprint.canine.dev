using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Sites.CreateSite;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Sites;

public sealed class CreateSiteTests
{
    [Fact]
    public async Task CreateSite_happy_path_appears_in_SiteOverview()
    {
        await using var host = new AuthoringTestHost();
        var siteId = SiteId.New();

        await host.Ok(new CreateSite(siteId, "My first site", "da-DK"));

        var site = host.Get<SiteOverview>().Current;
        Assert.NotNull(site);
        Assert.Equal(siteId, site.Id);
        Assert.Equal("My first site", site.Name);
        Assert.Equal(new Locale("da-DK"), site.DefaultLocale);
        Assert.Equal([new Locale("da-DK")], site.Locales);
    }

    [Fact]
    public async Task CreateSite_can_create_multiple_sites()
    {
        // Multi-site: an owner may create many sites; the first stays Current.
        await using var host = new AuthoringTestHost();
        await host.Ok(new CreateSite(SiteId.New(), "First", "en"));

        await host.Ok(new CreateSite(SiteId.New(), "Second", "en"));

        Assert.Equal(new[] { "First", "Second" }, host.Get<SiteOverview>().All.Select(s => s.Name).ToArray());
        Assert.Equal("First", host.Get<SiteOverview>().Current!.Name);
    }

    [Fact]
    public async Task CreateSite_with_empty_name_fails_validation()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateSite(SiteId.New(), "   ", "en"));

        Assert.Contains("name cannot be empty", error);
        Assert.Null(host.Get<SiteOverview>().Current);
    }

    [Fact]
    public async Task CreateSite_with_overlong_name_fails_validation()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateSite(SiteId.New(), new string('x', 101), "en"));

        Assert.Contains("100 characters", error);
    }

    [Fact]
    public async Task CreateSite_with_invalid_locale_fails_validation()
    {
        await using var host = new AuthoringTestHost();

        var error = await host.Fails(new CreateSite(SiteId.New(), "Site", "english!"));

        Assert.Contains("not a valid locale tag", error);
        Assert.Null(host.Get<SiteOverview>().Current);
    }
}
