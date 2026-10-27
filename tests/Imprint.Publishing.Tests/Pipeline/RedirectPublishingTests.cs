using Imprint.Authoring.Domain.Sites;
using Imprint.Authoring.Features.Sites.SetSiteRedirects;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// A site's redirect table, published: a small redirect page at each old address where no page lives,
/// never over a page that does, and swept away when the redirect is removed.
/// </summary>
public sealed class RedirectPublishingTests
{
    private const string Base = "https://watchdog.example";

    [Fact]
    public async Task An_old_address_with_no_page_gets_a_redirect_page_to_the_new_one()
    {
        await using var host = new PublishingTestHost(Base);
        var site = await host.CreateSite();
        await host.Ok(new SetSiteRedirects(site, [new SiteRedirect("/guide-read-only-by-design/", "https://resourcecenter.example/read-only-by-design/")]));

        await host.Publisher.Synchronize();

        var html = host.ReadText("guide-read-only-by-design/index.html");
        Assert.Contains("<meta http-equiv=\"refresh\" content=\"0; url=https://resourcecenter.example/read-only-by-design/\">", html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"canonical\" href=\"https://resourcecenter.example/read-only-by-design/\">", html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain("guide-read-only-by-design", host.ReadText("sitemap.xml"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_site_path_target_is_made_absolute_on_the_sites_own_origin()
    {
        await using var host = new PublishingTestHost(Base);
        var site = await host.CreateSite();
        await host.Ok(new SetSiteRedirects(site, [new SiteRedirect("/old/", "/new/")]));

        await host.Publisher.Synchronize();

        Assert.Contains("url=https://watchdog.example/new/", host.ReadText("old/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_redirect_never_replaces_a_published_page()
    {
        await using var host = new PublishingTestHost(Base);
        var site = await host.CreateSite();
        var page = await host.CreatePage(site, "about", "About us");
        await host.Publish(page);
        await host.Ok(new SetSiteRedirects(site, [new SiteRedirect("/about/", "/elsewhere/")]));

        await host.Publisher.Synchronize();

        Assert.DoesNotContain("http-equiv=\"refresh\"", host.ReadText("about/index.html"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Removing_a_redirect_sweeps_its_page_away()
    {
        await using var host = new PublishingTestHost(Base);
        var site = await host.CreateSite();
        await host.Ok(new SetSiteRedirects(site, [new SiteRedirect("/old/", "/new/")]));
        await host.Publisher.Synchronize();
        Assert.True(host.FileExists("old/index.html"));

        await host.Ok(new SetSiteRedirects(site, []));
        await host.Publisher.Synchronize();

        Assert.False(host.FileExists("old/index.html"));
    }
}
