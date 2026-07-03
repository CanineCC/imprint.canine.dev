using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.DeletePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class DeletePageTests
{
    [Fact]
    public async Task DeletePage_removes_the_page_from_the_page_list()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));
        await host.Ok(new CreatePage(PageId.New(), siteId, "Home", "home", "en"));

        await host.Ok(new DeletePage(pageId));

        Assert.Null(host.Get<PageList>().Get(pageId));
        Assert.Null(host.Get<PageDrafts>().Get(pageId));
    }

    [Fact]
    public async Task DeletePage_in_the_navigation_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));
        await host.Ok(new CreatePage(PageId.New(), siteId, "Home", "home", "en"));
        await PagesHost.SetNavigation(host, siteId, pageId);

        var error = await host.Fails(new DeletePage(pageId));
        Assert.Contains("Remove it from the navigation first.", error);
    }

    [Fact]
    public async Task DeletePage_of_the_only_page_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        var error = await host.Fails(new DeletePage(pageId));
        Assert.Contains("only page", error);
    }

    [Fact]
    public async Task DeletePage_of_an_unknown_page_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        await host.Ok(new CreatePage(PageId.New(), siteId, "Home", "home", "en"));

        var error = await host.Fails(new DeletePage(PageId.New()));
        Assert.Contains("no longer exists", error);
    }
}
