using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class CreatePageTests
{
    [Fact]
    public async Task CreatePage_lands_in_page_list_and_drafts()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();

        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var summary = host.Get<PageList>().Get(pageId);
        Assert.NotNull(summary);
        Assert.Equal("about", summary.Slug.Value);
        Assert.Equal("About", summary.Title.Get(new Locale("en")));
        Assert.Equal(PageStatus.Draft, summary.Status);

        var draft = host.Get<PageDrafts>().Get(pageId);
        Assert.NotNull(draft);
        Assert.Equal(siteId, draft.SiteId);
    }

    [Fact]
    public async Task CreatePage_with_empty_title_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);

        var error = await host.Fails(new CreatePage(PageId.New(), siteId, "  ", "about", "en"));
        Assert.Contains("title", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePage_with_malformed_slug_surfaces_the_slug_error()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);

        var error = await host.Fails(new CreatePage(PageId.New(), siteId, "About", "Bad Slug!", "en"));
        Assert.Contains("Slugs are 1–80 characters", error);
    }

    [Fact]
    public async Task CreatePage_with_malformed_locale_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);

        var error = await host.Fails(new CreatePage(PageId.New(), siteId, "About", "about", "not-a-locale"));
        Assert.Contains("not a valid locale tag", error);
    }

    [Fact]
    public async Task CreatePage_with_locale_not_on_the_site_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host, "en");

        var error = await host.Fails(new CreatePage(PageId.New(), siteId, "Om os", "om-siden", "da"));
        Assert.Contains("'da' is not one of this site's locales", error);
    }

    [Fact]
    public async Task CreatePage_with_taken_slug_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        await host.Ok(new CreatePage(PageId.New(), siteId, "About", "about", "en"));

        var error = await host.Fails(new CreatePage(PageId.New(), siteId, "Also About", "about", "en"));
        Assert.Contains("already used by another page", error);
    }

    [Fact]
    public async Task CreatePage_for_unknown_site_is_rejected()
    {
        await using var host = PagesHost.Create();
        await PagesHost.SeedSite(host);

        var error = await host.Fails(new CreatePage(PageId.New(), SiteId.New(), "About", "about", "en"));
        Assert.Contains("site no longer exists", error);
    }
}
