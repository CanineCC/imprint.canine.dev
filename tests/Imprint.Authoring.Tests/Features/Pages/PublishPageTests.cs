using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddPreset;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Pages.UnpublishPage;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class PublishPageTests
{
    private static readonly Locale En = new("en");

    [Fact]
    public async Task PublishPage_snapshot_matches_the_draft_at_that_moment_and_does_not_follow_later_edits()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new AddPreset(pageId, 0, "hero"));

        await host.Ok(new PublishPage(pageId));

        var draft = host.Get<PageDrafts>().Get(pageId)!;
        var snapshot = host.Get<PublishedContent>().Get(pageId);
        Assert.NotNull(snapshot);
        Assert.Equal(draft.Tree, snapshot.Tree);
        Assert.Equal(draft.Title, snapshot.Title);
        Assert.Equal(PageStatus.Published, host.Get<PageList>().Get(pageId)!.Status);

        // Edit the draft: the published snapshot must NOT move.
        var headingId = draft.Tree.All().OfType<HeadingNode>().First().Id;
        await host.Ok(new EditText(pageId, headingId, "text", "en", "New words"));

        var snapshotAfterEdit = host.Get<PublishedContent>().Get(pageId)!;
        var publishedHeading = (HeadingNode)snapshotAfterEdit.Tree.Find(headingId)!;
        Assert.Equal("Make something people remember", publishedHeading.Text.Get(En));

        var draftHeading = (HeadingNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(headingId)!;
        Assert.Equal("New words", draftHeading.Text.Get(En));
        Assert.Equal(PageStatus.Modified, host.Get<PageList>().Get(pageId)!.Status);
    }

    [Fact]
    public async Task PublishPage_with_nothing_new_to_publish_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new PublishPage(pageId));

        var error = await host.Fails(new PublishPage(pageId));
        Assert.Contains("no changes to publish", error);
    }

    [Fact]
    public async Task UnpublishPage_removes_the_published_snapshot()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new PublishPage(pageId));

        await host.Ok(new UnpublishPage(pageId));

        Assert.Null(host.Get<PublishedContent>().Get(pageId));
        Assert.Equal(PageStatus.Draft, host.Get<PageList>().Get(pageId)!.Status);
    }

    [Fact]
    public async Task UnpublishPage_of_a_never_published_page_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));

        var error = await host.Fails(new UnpublishPage(pageId));
        Assert.Contains("not published", error);
    }

    [Fact]
    public async Task PublishPage_after_more_edits_updates_the_snapshot()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new AddPreset(pageId, 0, "hero"));
        await host.Ok(new PublishPage(pageId));

        var headingId = host.Get<PageDrafts>().Get(pageId)!
            .Tree.All().OfType<HeadingNode>().First().Id;
        await host.Ok(new EditText(pageId, headingId, "text", "en", "Second edition"));
        await host.Ok(new PublishPage(pageId));

        var published = (HeadingNode)host.Get<PublishedContent>().Get(pageId)!.Tree.Find(headingId)!;
        Assert.Equal("Second edition", published.Text.Get(En));
        Assert.Equal(PageStatus.Published, host.Get<PageList>().Get(pageId)!.Status);
    }
}
