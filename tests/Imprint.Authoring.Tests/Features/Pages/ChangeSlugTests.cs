using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages.ChangeSlug;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class ChangeSlugTests
{
    [Fact]
    public async Task ChangeSlug_updates_the_page_list()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        await host.Ok(new ChangeSlug(pageId, "about-us"));

        Assert.Equal("about-us", host.Get<PageList>().Get(pageId)!.Slug.Value);
    }

    [Fact]
    public async Task ChangeSlug_to_a_taken_slug_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));
        await host.Ok(new CreatePage(PageId.New(), siteId, "Team", "team", "en"));

        var error = await host.Fails(new ChangeSlug(pageId, "team"));
        Assert.Contains("already used by another page", error);
    }

    [Fact]
    public async Task ChangeSlug_to_the_pages_own_slug_is_a_no_op_not_a_collision()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        // Self-exclusion in SlugTaken: keeping the current slug must succeed…
        await host.Ok(new ChangeSlug(pageId, "about"));

        // …and raise nothing (the aggregate treats it as no change).
        var events = await host.Store.ReadStream(pageId.Stream);
        Assert.Single(events);
    }

    [Fact]
    public async Task ChangeSlug_with_malformed_slug_surfaces_the_slug_error()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangeSlug(pageId, "-nope-"));
        Assert.Contains("Slugs are 1–80 characters", error);
    }

    [Fact]
    public async Task ChangeSlug_to_a_reserved_name_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangeSlug(pageId, "assets"));
        Assert.Contains("reserved", error);
    }
}
