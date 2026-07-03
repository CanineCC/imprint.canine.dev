using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.ChangePageMeta;
using Imprint.Authoring.Features.Pages.CreatePage;
using Imprint.Authoring.Features.Pages.EditText;
using Imprint.Authoring.Features.Pages.PublishAllStale;
using Imprint.Authoring.Features.Sites.CreateSiteFromTemplate;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class PagesJourneyTests
{
    private static readonly Locale En = new("en");

    /// <summary>
    /// The whole editing story through the Pages slices: enact the "launch" template
    /// recipe (create pages, add its sections, wire navigation), edit, publish all —
    /// then the delivery plane's read model has every page, exactly as edited.
    /// </summary>
    [Fact]
    public async Task Full_journey_site_from_template_edit_publish_all()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var template = SiteTemplates.Find("launch")!;

        // Create the site's pages from the template — a template is nothing special,
        // just a recipe of ordinary commands (the seed-stream pattern).
        var pagesBySlug = new Dictionary<string, PageId>();
        var navigation = new List<PageId>();
        foreach (var templatePage in template.Pages)
        {
            var pageId = PageId.New();
            await host.Ok(new CreatePage(pageId, siteId, templatePage.Title, templatePage.Slug, "en"));

            var sections = templatePage.BuildSections(En);
            for (var index = 0; index < sections.Count; index++)
            {
                await host.Ok(new AddNode(pageId, NodeId.Root, index, sections[index]));
            }

            if (templatePage.InNavigation)
            {
                navigation.Add(pageId);
            }

            pagesBySlug[templatePage.Slug] = pageId;
        }

        await PagesHost.SetNavigation(host, siteId, [.. navigation]);

        // Edit before going live.
        var homeId = pagesBySlug["home"];
        var homeHeadingId = host.Get<PageDrafts>().Get(homeId)!.Tree.All().OfType<HeadingNode>().First().Id;
        await host.Ok(new EditText(homeId, homeHeadingId, "text", "en", "We ship on Fridays"));
        await host.Ok(new ChangePageMeta(homeId, "en", "Launch — Home", "The little site that could."));

        // One command publishes everything stale.
        await host.Ok(new PublishAllStale());

        // The delivery plane sees every page…
        var published = host.Get<PublishedContent>();
        Assert.Equal(template.Pages.Count, published.All.Count);
        foreach (var templatePage in template.Pages)
        {
            var snapshot = published.Get(pagesBySlug[templatePage.Slug]);
            Assert.NotNull(snapshot);
            Assert.Equal(templatePage.Slug, snapshot.Slug.Value);
            Assert.Equal(
                host.Get<PageDrafts>().Get(pagesBySlug[templatePage.Slug])!.Tree,
                snapshot.Tree);
        }

        // …with the edits included, and the dashboard shows everything as published.
        var publishedHome = published.Get(homeId)!;
        var publishedHeading = (HeadingNode)publishedHome.Tree.Find(homeHeadingId)!;
        Assert.Equal("We ship on Fridays", publishedHeading.Text.Get(En));
        Assert.Equal("Launch — Home", publishedHome.MetaTitle.Get(En));
        Assert.All(host.Get<PageList>().All(), summary => Assert.Equal(PageStatus.Published, summary.Status));

        // Navigation order made it to the read model: home is the home page.
        Assert.Equal(homeId, host.Get<PageList>().Home!.Id);
    }
}
