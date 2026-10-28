using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Pages;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.PublishAllStale;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Posts.ChangePostBody;
using Imprint.Authoring.Features.Posts.CreatePost;
using Imprint.Authoring.Features.Posts.PublishPost;
using Imprint.Authoring.Features.Sites.ArchiveSite;
using Imprint.Authoring.Projections;
using Imprint.Authoring.Tests.Features.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Authoring.Tests.Features.Sites;

/// <summary>
/// A retired site is archived, not deleted: its history stays, but it leaves the lists people and agents pick from
/// and nothing of it can go live again until it is restored.
/// </summary>
public sealed class ArchiveSiteTests
{
    [Fact]
    public async Task An_archived_site_leaves_the_active_list_and_comes_back_when_restored()
    {
        await using var host = NewHost();
        var kept = await host.CreateTestSite();
        var retired = await host.CreateTestSite();

        await host.Ok(new ArchiveSite(retired));
        await host.CatchUp();

        var sites = host.Get<SiteOverview>();
        Assert.Equal([kept], sites.Active.Select(s => s.Id));
        Assert.Equal([retired], sites.Archived.Select(s => s.Id));
        Assert.Equal(2, sites.All.Count);

        await host.Ok(new RestoreSite(retired));
        await host.CatchUp();

        Assert.Equal([kept, retired], sites.Active.Select(s => s.Id));
        Assert.Empty(sites.Archived);
    }

    [Fact]
    public async Task Nothing_on_an_archived_site_can_be_published()
    {
        await using var host = NewHost();
        var site = await host.CreateTestSite();
        var page = PageId.New();
        await host.Ok(new CreatePage(page, site, "Home", "home", "en"));
        var post = PostId.New();
        await host.Ok(new CreatePost(post, site, "Hello", "hello", "en"));
        await host.Ok(new ChangePostBody(post, "en", "# Hello\n\nProse."));

        await host.Ok(new ArchiveSite(site));

        Assert.Contains("archived", await host.Fails(new PublishPage(page)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("archived", await host.Fails(new PublishPost(post, "en")), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("archived", await host.Fails(new PublishAllStale(site)), StringComparison.OrdinalIgnoreCase);

        await host.Ok(new RestoreSite(site));
        await host.Ok(new PublishPage(page));
        await host.Ok(new PublishPost(post, "en"));
    }

    private static AuthoringTestHost NewHost() =>
        new(services => services.AddSingleton<IWidgetCatalog>(new FakeWidgetCatalog()));
}
