using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages.ChangePageMeta;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class ChangePageMetaTests
{
    [Fact]
    public async Task ChangePageMeta_lands_in_the_draft()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        await host.Ok(new ChangePageMeta(pageId, "en", "About us — Imprint", "What we build and why."));

        var draft = host.Get<PageDrafts>().Get(pageId)!;
        Assert.Equal("About us — Imprint", draft.MetaTitle.Get(new Locale("en")));
        Assert.Equal("What we build and why.", draft.MetaDescription.Get(new Locale("en")));
    }

    [Fact]
    public async Task ChangePageMeta_with_locale_not_on_the_site_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host, "en");
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangePageMeta(pageId, "da", "Om os", null));
        Assert.Contains("'da' is not one of this site's locales", error);
    }

    [Fact]
    public async Task ChangePageMeta_over_the_length_limit_surfaces_the_domain_error()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "About", "about", "en"));

        var error = await host.Fails(new ChangePageMeta(pageId, "en", null, new string('x', 301)));
        Assert.Contains("limited to 300 characters", error);
    }
}
