using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.ChangeNavigation;
using Imprint.Authoring.Features.Sites.SetHomePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Sites;

/// <summary>
/// The site root used to BE a menu position — PageSummary.IsHome was NavigationOrder == 0.
/// On 2026-09-03 that shipped: "Home" was dropped from watchdog.canine.dev's navigation to
/// make room for another group, and "/" silently began serving the next top-level link
/// (Pricing), which simultaneously lost its own /pricing/ URL. These tests pin the fix.
/// </summary>
public sealed class SetHomePageTests
{
    [Fact]
    public async Task Without_an_explicit_home_page_the_nav_first_page_is_still_home()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId, "home", "Home");
        var pricing = await host.CreateTestPage(siteId, "pricing", "Pricing");

        await host.Ok(new ChangeNavigation(siteId, [
            NavigationItem.Page(home, null),
            NavigationItem.Page(pricing, null),
        ]));

        var pages = host.Get<PageList>();
        Assert.True(pages.Get(home)!.IsHome);
        Assert.False(pages.Get(pricing)!.IsHome);
    }

    [Fact]
    public async Task An_explicit_home_page_survives_being_removed_from_the_navigation()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId, "home", "Home");
        var pricing = await host.CreateTestPage(siteId, "pricing", "Pricing");

        await host.Ok(new ChangeNavigation(siteId, [
            NavigationItem.Page(home, null),
            NavigationItem.Page(pricing, null),
        ]));
        await host.Ok(new SetHomePage(siteId, home));

        // The regression: the brand mark already links home, so "Home" is dropped from the
        // menu and Pricing becomes the first top-level link.
        await host.Ok(new ChangeNavigation(siteId, [NavigationItem.Page(pricing, null)]));

        var pages = host.Get<PageList>();
        Assert.True(pages.Get(home)!.IsHome);
        Assert.False(pages.Get(pricing)!.IsHome);
        Assert.Equal(home, pages.HomeOf(siteId)!.Id);

        // …and the page that merely became first keeps its own slug rather than the root.
        Assert.Equal(0, pages.Get(pricing)!.NavigationOrder);
    }

    [Fact]
    public async Task Clearing_the_home_page_restores_the_nav_first_rule()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var home = await host.CreateTestPage(siteId, "home", "Home");
        var pricing = await host.CreateTestPage(siteId, "pricing", "Pricing");

        await host.Ok(new ChangeNavigation(siteId, [
            NavigationItem.Page(pricing, null),
            NavigationItem.Page(home, null),
        ]));
        await host.Ok(new SetHomePage(siteId, home));
        Assert.True(host.Get<PageList>().Get(home)!.IsHome);

        await host.Ok(new SetHomePage(siteId, null));

        var pages = host.Get<PageList>();
        Assert.True(pages.Get(pricing)!.IsHome);
        Assert.False(pages.Get(home)!.IsHome);
    }

    [Fact]
    public async Task A_home_page_from_another_site_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var otherSite = await host.CreateTestSite();
        var foreign = await host.CreateTestPage(otherSite, "elsewhere", "Elsewhere");

        var error = await host.Fails(new SetHomePage(siteId, foreign));

        Assert.Contains("page of this site", error);
    }

    [Fact]
    public async Task An_unknown_home_page_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();

        var error = await host.Fails(new SetHomePage(siteId, PageId.New()));

        Assert.Contains("no longer exists", error);
    }
}
