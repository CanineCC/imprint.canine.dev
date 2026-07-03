using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Pages.AddNode;
using Imprint.Authoring.Features.Pages.DetachBlockInstance;
using Imprint.Authoring.Features.Pages.SetBlockOverride;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Pages;

public sealed class DetachBlockInstanceTests
{
    private static readonly Locale En = new("en");

    [Fact]
    public async Task DetachBlockInstance_materializes_resolved_content_with_fresh_ids()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var heading = new HeadingNode { Id = NodeId.New(), Text = LocalizedText.Of(En, "Block heading") };
        var copy = new RichTextNode { Id = NodeId.New(), Html = LocalizedText.Of(En, "<p>Block copy</p>") };
        var spec = new StackNode { Id = NodeId.New(), Children = NodeList.Of(heading, copy) };
        var definition = await PagesHost.SeedBlock(host, "Card", spec);

        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var instance = new BlockInstanceNode { Id = NodeId.New(), DefinitionId = definition.Id };
        await host.Ok(new AddNode(pageId, sectionId, 0, instance));
        await host.Ok(new SetBlockOverride(pageId, instance.Id, heading.Id, "text", "en", "My own words"));

        await host.Ok(new DetachBlockInstance(pageId, instance.Id));

        var tree = host.Get<PageDrafts>().Get(pageId)!.Tree;
        Assert.Null(tree.Find(instance.Id));

        // The materialized subtree sits where the instance was, override applied…
        var section = (SectionNode)tree.Find(sectionId)!;
        var stack = Assert.IsType<StackNode>(Assert.Single(section.Children));
        var materializedHeading = Assert.IsType<HeadingNode>(stack.Children[0]);
        Assert.Equal("My own words", materializedHeading.Text.Get(En));
        var materializedCopy = Assert.IsType<RichTextNode>(stack.Children[1]);
        Assert.Equal("<p>Block copy</p>", materializedCopy.Html.Get(En));

        // …and its ids are disjoint from the definition's, so future definition edits
        // cannot bleed into content the user just took ownership of.
        var definitionIds = PageTree.Flatten(spec).Select(node => node.Id).ToHashSet();
        var materializedIds = PageTree.Flatten(stack).Select(node => node.Id).ToHashSet();
        Assert.Empty(definitionIds.Intersect(materializedIds));
    }

    [Fact]
    public async Task DetachBlockInstance_with_a_deleted_definition_suggests_removal()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, sectionId) = await PagesHost.SeedPageWithSection(host, siteId);
        var instance = new BlockInstanceNode { Id = NodeId.New(), DefinitionId = BlockDefinitionId.New() };
        await host.Ok(new AddNode(pageId, sectionId, 0, instance));

        var error = await host.Fails(new DetachBlockInstance(pageId, instance.Id));
        Assert.Contains("remove the element instead", error);
    }

    [Fact]
    public async Task DetachBlockInstance_of_an_unknown_instance_is_rejected()
    {
        await using var host = PagesHost.Create();
        var siteId = await PagesHost.SeedSite(host);
        var (pageId, _) = await PagesHost.SeedPageWithSection(host, siteId);

        var error = await host.Fails(new DetachBlockInstance(pageId, NodeId.New()));
        Assert.Contains("block instance no longer exists", error);
    }
}
