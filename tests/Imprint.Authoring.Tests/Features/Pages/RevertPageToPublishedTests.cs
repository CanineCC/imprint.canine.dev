using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddPreset;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Features.Pages.PublishPage;
using Imprint.Authoring.Features.Pages.RevertPageToPublished;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

/// <summary>
/// "Discard changes": revert a page's draft content to the last published snapshot, and
/// its undo (RestorePageContent) that brings the discarded work back.
/// </summary>
public sealed class RevertPageToPublishedTests
{
    private static readonly Locale En = new("en");

    [Fact]
    public async Task Revert_restores_the_published_content_and_discards_intervening_edits()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new AddPreset(pageId, 0, "hero"));
        await host.Ok(new PublishPage(pageId));

        var headingId = host.Get<PageDrafts>().Get(pageId)!.Tree.All().OfType<HeadingNode>().First().Id;
        var publishedText = ((HeadingNode)host.Get<PublishedContent>().Get(pageId)!.Tree.Find(headingId)!).Text.Get(En);

        // Edit the draft away from the published version.
        await host.Ok(new EditText(pageId, headingId, "text", "en", "A hasty change"));
        Assert.Equal("A hasty change", ((HeadingNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(headingId)!).Text.Get(En));

        await host.Ok(new RevertPageToPublished(pageId));

        // The draft now matches the published snapshot exactly.
        var draft = host.Get<PageDrafts>().Get(pageId)!;
        Assert.Equal(host.Get<PublishedContent>().Get(pageId)!.Tree, draft.Tree);
        Assert.Equal(publishedText, ((HeadingNode)draft.Tree.Find(headingId)!).Text.Get(En));
    }

    [Fact]
    public async Task Revert_of_a_never_published_page_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new AddPreset(pageId, 0, "hero"));

        var error = await host.Fails(new RevertPageToPublished(pageId));
        Assert.Contains("never been published", error);
    }

    [Fact]
    public async Task Revert_is_a_no_op_when_the_draft_already_equals_the_published_version()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new AddPreset(pageId, 0, "hero"));
        await host.Ok(new PublishPage(pageId));

        var versionBefore = host.Get<PageList>().Get(pageId)!.Version;
        await host.Ok(new RevertPageToPublished(pageId)); // nothing to discard
        Assert.Equal(versionBefore, host.Get<PageList>().Get(pageId)!.Version);
    }

    [Fact]
    public async Task RestorePageContent_undoes_a_revert_by_bringing_the_discarded_draft_back()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var pageId = PageId.New();
        await host.Ok(new CreatePage(pageId, siteId, "Home", "home", "en"));
        await host.Ok(new AddPreset(pageId, 0, "hero"));
        await host.Ok(new PublishPage(pageId));

        var headingId = host.Get<PageDrafts>().Get(pageId)!.Tree.All().OfType<HeadingNode>().First().Id;
        await host.Ok(new EditText(pageId, headingId, "text", "en", "A hasty change"));

        // Capture the pre-revert draft (what the editor computes as the undo inverse).
        var preRevertRoots = host.Get<PageDrafts>().Get(pageId)!.Tree.Roots;

        await host.Ok(new RevertPageToPublished(pageId));
        Assert.NotEqual("A hasty change", ((HeadingNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(headingId)!).Text.Get(En));

        // Undo the revert.
        await host.Ok(new RestorePageContent(pageId, preRevertRoots));
        Assert.Equal("A hasty change", ((HeadingNode)host.Get<PageDrafts>().Get(pageId)!.Tree.Find(headingId)!).Text.Get(En));
    }
}
