using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.ChangeNavigation;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Sites;

public sealed class ChangeNavigationTests
{
    [Fact]
    public async Task ChangeNavigation_happy_path_updates_SiteOverview_and_PageList_order()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId, "home", "Home");
        var about = await host.CreateTestPage(siteId, "about", "About");

        await host.Ok(new ChangeNavigation(siteId, [
            new NavigationItem(home, null),
            new NavigationItem(about, LocalizedText.Of(new Locale("en"), "Who we are")),
        ]));

        var site = host.Get<SiteOverview>().Current!;
        Assert.Equal([home, about], site.Navigation.Select(item => item.PageId));

        var pageList = host.Get<PageList>();
        Assert.Equal(0, pageList.Get(home)!.NavigationOrder);
        Assert.True(pageList.Get(home)!.IsHome);
        Assert.Equal(1, pageList.Get(about)!.NavigationOrder);
    }

    [Fact]
    public async Task ChangeNavigation_with_unknown_page_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId);

        var error = await host.Fails(new ChangeNavigation(siteId, [
            new NavigationItem(home, null),
            new NavigationItem(PageId.New(), null),
        ]));

        Assert.Contains("no longer exists", error);
        Assert.Empty(host.Get<SiteOverview>().Current!.Navigation);
    }

    [Fact]
    public async Task ChangeNavigation_with_a_deleted_page_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var page = await host.CreateTestPage(siteId);
        await host.MutatePage(page, p => p.Delete());

        var error = await host.Fails(new ChangeNavigation(siteId, [new NavigationItem(page, null)]));

        Assert.Contains("no longer exists", error);
    }

    [Fact]
    public async Task ChangeNavigation_with_the_same_page_twice_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId);

        var error = await host.Fails(new ChangeNavigation(siteId, [
            new NavigationItem(home, null),
            new NavigationItem(home, null),
        ]));

        Assert.Contains("same page twice", error);
    }

    [Fact]
    public async Task ChangeNavigation_can_clear_the_navigation()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId);
        await host.Ok(new ChangeNavigation(siteId, [new NavigationItem(home, null)]));

        await host.Ok(new ChangeNavigation(siteId, []));

        Assert.Empty(host.Get<SiteOverview>().Current!.Navigation);
        Assert.Null(host.Get<PageList>().Get(home)!.NavigationOrder);
    }
}
