using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// Which page the publisher writes to "/". This used to be "the first top-level page link
/// in the navigation" and nothing else, so on 2026-09-03 dropping "Home" from
/// watchdog.canine.dev's menu — the brand mark already links home — moved the site root
/// onto Pricing and took /pricing/ away with it. Nothing errored; the front page just
/// quietly changed. These tests pin the explicit choice, and the fallback for every site
/// that has never made one.
/// </summary>
public sealed class HomePageRoutingTests
{
    private static async Task<(PublishingTestHost Host, SiteId Site, PageId Home, PageId Pricing)> Setup()
    {
        var host = new PublishingTestHost();
        var site = await host.CreateSite("Watchdog");
        var home = await host.CreatePage(site, "home", "Home");
        var pricing = await host.CreatePage(site, "pricing", "Pricing");
        await host.Publish(home);
        await host.Publish(pricing);
        return (host, site, home, pricing);
    }

    [Fact]
    public async Task Without_an_explicit_choice_the_nav_first_page_is_the_root()
    {
        var (host, site, home, pricing) = await Setup();
        await using var _ = host;

        await host.SetNavigation(site, home, pricing);
        await host.Publisher.Synchronize();

        Assert.True(host.FileExists("index.html"));
        Assert.True(host.FileExists("pricing/index.html"));
        Assert.False(host.FileExists("home/index.html"));
    }

    [Fact]
    public async Task An_explicit_home_page_keeps_the_root_when_it_leaves_the_navigation()
    {
        var (host, site, home, pricing) = await Setup();
        await using var _ = host;

        await host.SetHomePage(site, home);
        // The regression: "Home" is dropped from the menu, so Pricing becomes the only
        // top-level page link — and used to inherit "/" along with it.
        await host.SetNavigation(site, pricing);
        await host.Publisher.Synchronize();

        Assert.True(host.FileExists("index.html"));
        Assert.Contains("Home", host.ReadText("index.html"));
        // …and Pricing keeps its own URL rather than being swallowed by the root.
        Assert.True(host.FileExists("pricing/index.html"));
    }

    [Fact]
    public async Task An_explicit_home_page_outranks_the_nav_first_page()
    {
        var (host, site, home, pricing) = await Setup();
        await using var _ = host;

        await host.SetNavigation(site, pricing, home);
        await host.SetHomePage(site, home);

        await host.Publisher.Synchronize();

        Assert.Contains("Home", host.ReadText("index.html"));
        Assert.True(host.FileExists("pricing/index.html"));
        Assert.False(host.FileExists("home/index.html"));
    }

    [Fact]
    public async Task Clearing_the_choice_returns_the_root_to_the_nav_first_page()
    {
        var (host, site, home, pricing) = await Setup();
        await using var _ = host;

        await host.SetNavigation(site, pricing, home);
        await host.SetHomePage(site, home);
        await host.SetHomePage(site, null);

        await host.Publisher.Synchronize();

        Assert.Contains("Pricing", host.ReadText("index.html"));
        Assert.True(host.FileExists("home/index.html"));
    }
}
