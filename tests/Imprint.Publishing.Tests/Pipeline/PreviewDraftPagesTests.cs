using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The preview plane's page source: a target with <c>IncludeDrafts</c> renders every page
/// from its CURRENT DRAFT tree — published pages with unpublished edits, and pages never
/// published at all — while the real publish target keeps rendering only what was approved.
/// This is the regression guard for the bug where /preview honoured draft POSTS but silently
/// rendered pages from the published projection, so page edits never showed up in a preview.
/// </summary>
public sealed class PreviewDraftPagesTests
{
    [Fact]
    public async Task A_preview_target_renders_the_draft_tree_and_the_publish_target_does_not()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, Section("Published hero"));
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);
        await host.Publisher.Synchronize();

        // A draft-only edit on a published page.
        await host.AddSection(homeId, Section("Draft hero"));

        // The real publish target still shows exactly what was approved.
        await host.Publisher.Synchronize();
        var published = host.ReadText("index.html");
        Assert.Contains("Published hero", published, StringComparison.Ordinal);
        Assert.DoesNotContain("Draft hero", published, StringComparison.Ordinal);

        // The preview target shows the tree the author just edited.
        var previewPath = Path.Combine(host.Root, "preview");
        await host.Publisher.Synchronize(PreviewTarget(host, siteId, previewPath));
        var preview = File.ReadAllText(Path.Combine(previewPath, "index.html"));
        Assert.Contains("Published hero", preview, StringComparison.Ordinal);
        Assert.Contains("Draft hero", preview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_preview_target_renders_a_page_that_was_never_published()
    {
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, Section("Home hero"));
        var draftOnly = await host.CreatePage(siteId, "launch", "Launch");
        await host.AddSection(draftOnly, Section("Unreleased launch page"));
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        await host.Publisher.Synchronize();
        Assert.False(host.FileExists("launch/index.html"));

        var previewPath = Path.Combine(host.Root, "preview");
        await host.Publisher.Synchronize(PreviewTarget(host, siteId, previewPath));
        var preview = File.ReadAllText(Path.Combine(previewPath, "launch/index.html"));
        Assert.Contains("Unreleased launch page", preview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_second_preview_pass_rerenders_a_page_edited_since_the_first()
    {
        // The staleness check compares manifest versions; a draft edit must move the
        // version the preview manifest records, or the second pass would skip the page.
        await using var host = new PublishingTestHost();
        var siteId = await host.CreateSite();
        var homeId = await host.CreatePage(siteId, "home", "Home");
        await host.AddSection(homeId, Section("First draft"));
        await host.SetNavigation(siteId, homeId);
        await host.Publish(homeId);

        var previewPath = Path.Combine(host.Root, "preview");
        await host.Publisher.Synchronize(PreviewTarget(host, siteId, previewPath));

        await host.AddSection(homeId, Section("Second thought"));
        await host.Publisher.Synchronize(PreviewTarget(host, siteId, previewPath));

        var preview = File.ReadAllText(Path.Combine(previewPath, "index.html"));
        Assert.Contains("Second thought", preview, StringComparison.Ordinal);
    }

    private static PublishTarget PreviewTarget(PublishingTestHost host, SiteId siteId, string outputPath)
    {
        var site = host.Services.GetRequiredService<SiteOverview>().Get(siteId);
        Assert.NotNull(site);
        return new PublishTarget(site!, outputPath, BaseUrl: null, IncludeDrafts: true);
    }

    private static SectionNode Section(string heading) => new()
    {
        Id = NodeId.New(),
        Children = NodeList.Of(new HeadingNode
        {
            Id = NodeId.New(),
            Text = LocalizedText.Of(PublishingTestHost.En, heading),
        }),
    };
}
