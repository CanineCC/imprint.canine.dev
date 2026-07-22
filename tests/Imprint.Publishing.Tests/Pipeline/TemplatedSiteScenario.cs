using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages;

namespace Imprint.Publishing.Tests.Pipeline;

/// <summary>
/// The shared end-to-end scenario: a two-locale site built from the section presets
/// (the same specs the launch template uses) with an image, a widget and a block
/// instance on the home page — every moving part of the delivery contract in one site.
/// </summary>
internal sealed record TemplatedSiteScenario(
    SiteId SiteId,
    PageId HomeId,
    PageId AboutId,
    AssetId ImageId,
    BlockDefinitionId BlockId)
{
    public static async Task<TemplatedSiteScenario> Build(PublishingTestHost host)
    {
        // Two widgets on disk, only one used — the unused bundle must never publish.
        host.WriteWidgets(
            ("x-note", "export default class XNote extends HTMLElement {}\n"),
            ("x-extra", "export default class XExtra extends HTMLElement {}\n"));

        var siteId = await host.CreateSite("Acme Studio", "en");
        await host.AddLocale(siteId, "da");

        var blockId = await host.DefineBlock("Promo", new StackNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new HeadingNode
            {
                Id = NodeId.New(),
                Level = 2,
                Text = LocalizedText.Of(PublishingTestHost.En, "Reusable promo block"),
            }),
        });
        var imageId = await host.CreateImageAsset();

        var homeId = await host.CreatePage(siteId, "home", "Home");
        foreach (var preset in (string[])["hero", "feature-grid", "split", "cta", "footer"])
        {
            await host.AddSection(homeId, SectionPresets.Find(preset)!.Build(PublishingTestHost.En));
        }

        await host.AddSection(homeId, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new ImageNode { Id = NodeId.New(), AssetId = imageId, Alt = LocalizedText.Of(PublishingTestHost.En, "A studio photo") },
                new ImageNode { Id = NodeId.New(), AssetId = imageId },
                new WidgetNode
                {
                    Id = NodeId.New(),
                    Tag = "x-note",
                    Props = PropBag.Of([new KeyValuePair<string, string>("text", "hello")]),
                },
                new BlockInstanceNode { Id = NodeId.New(), DefinitionId = blockId }),
        });
        await host.SetTitle(homeId, "da", "Hjem");

        var aboutId = await host.CreatePage(siteId, "about", "About");
        await host.AddSection(aboutId, SectionPresets.Find("text")!.Build(PublishingTestHost.En));
        await host.SetTitle(aboutId, "da", "Om os");
        await host.SetMeta(aboutId, "en", null, "About Acme Studio.");

        await host.SetNavigation(siteId, homeId, aboutId);
        await host.Publish(homeId);
        await host.Publish(aboutId);
        return new TemplatedSiteScenario(siteId, homeId, aboutId, imageId, blockId);
    }
}
