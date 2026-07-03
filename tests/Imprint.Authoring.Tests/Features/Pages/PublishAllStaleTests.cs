using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddPreset;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Features.Pages.PublishAllStale;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class PublishAllStaleTests
{
    [Fact]
    public async Task PublishAllStale_publishes_drafts_and_modified_pages_and_skips_published_ones()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);

        var draftId = PageId.New();
        await host.Ok(new CreatePage(draftId, siteId, "Draft", "draft", "en"));

        var modifiedId = PageId.New();
        await host.Ok(new CreatePage(modifiedId, siteId, "Modified", "modified", "en"));
        await host.Ok(new AddPreset(modifiedId, 0, "hero"));
        await host.Ok(new PublishPage(modifiedId));
        var headingId = host.Get<PageDrafts>().Get(modifiedId)!.Tree.All().OfType<HeadingNode>().First().Id;
        await host.Ok(new EditText(modifiedId, headingId, "text", "en", "Fresher words"));

        var publishedId = PageId.New();
        await host.Ok(new CreatePage(publishedId, siteId, "Steady", "steady", "en"));
        await host.Ok(new PublishPage(publishedId));
        var steadyVersionBefore = host.Get<PageList>().Get(publishedId)!.PublishedVersion;

        await host.Ok(new PublishAllStale());

        var published = host.Get<PublishedContent>();
        Assert.NotNull(published.Get(draftId));
        var modifiedHeading = (HeadingNode)published.Get(modifiedId)!.Tree.Find(headingId)!;
        Assert.Equal("Fresher words", modifiedHeading.Text.Get(new Locale("en")));

        // The already-published page was skipped: same publish decision as before.
        Assert.Equal(steadyVersionBefore, host.Get<PageList>().Get(publishedId)!.PublishedVersion);
        Assert.All(host.Get<PageList>().All(), summary => Assert.Equal(PageStatus.Published, summary.Status));
    }

    [Fact]
    public async Task PublishAllStale_with_nothing_stale_is_ok()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new PublishPage(pageId));

        await host.Ok(new PublishAllStale());
    }

    [Fact]
    public async Task PublishAllStale_collects_per_page_failures_but_still_publishes_the_rest()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);

        var goodId = PageId.New();
        await host.Ok(new CreatePage(goodId, siteId, "Good", "good", "en"));
        var doomedId = PageId.New();
        await host.Ok(new CreatePage(doomedId, siteId, "Doomed", "doomed", "en"));

        // Delete the doomed page behind the projections' back — the exact projection-
        // lag race the handler's comment names: PageList still lists it as stale, the
        // stream already says deleted.
        var store = host.Get<IAggregateStore>();
        var doomed = await store.Load<Page>(doomedId.Stream);
        doomed.Delete();
        await store.Save(doomed);

        var error = await host.Fails(new PublishAllStale());
        Assert.Contains("doomed: This page has been deleted.", error);
        Assert.DoesNotContain("good:", error);

        // The good page's publish committed regardless of its sibling's failure; a
        // failed dispatch skips projection catch-up, so catch up before asserting.
        await host.Get<ProjectionEngine>().CatchUp();
        Assert.NotNull(host.Get<PublishedContent>().Get(goodId));
        Assert.Null(host.Get<PublishedContent>().Get(doomedId));
    }
}
