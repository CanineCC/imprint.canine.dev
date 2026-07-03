using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages.ChangePageTitle;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class ChangePageTitleTests
{
    [Fact]
    public async Task ChangePageTitle_updates_the_page_list_title()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        await host.Ok(new ChangePageTitle(pageId, "en", "About us"));

        Assert.Equal("About us", host.Get<PageList>().Get(pageId)!.Title.Get(new Locale("en")));
    }

    [Fact]
    public async Task ChangePageTitle_in_second_site_locale_adds_a_translation()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host, "en", "da");
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        await host.Ok(new ChangePageTitle(pageId, "da", "Om os"));

        var title = host.Get<PageList>().Get(pageId)!.Title;
        Assert.Equal("About", title.Get(new Locale("en")));
        Assert.Equal("Om os", title.Get(new Locale("da")));
    }

    [Fact]
    public async Task ChangePageTitle_with_locale_not_on_the_site_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host, "en");
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangePageTitle(pageId, "da", "Om os"));
        Assert.Contains("'da' is not one of this site's locales", error);
    }

    [Fact]
    public async Task ChangePageTitle_with_malformed_locale_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangePageTitle(pageId, "english", "About us"));
        Assert.Contains("not a valid locale tag", error);
    }

    [Fact]
    public async Task ChangePageTitle_over_the_length_limit_surfaces_the_domain_error()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangePageTitle(pageId, "en", new string('x', 201)));
        Assert.Contains("limited to 200 characters", error);
    }
}
