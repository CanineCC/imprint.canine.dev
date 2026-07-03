using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Blocks.DefineBlockFromNode;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Blocks;

public sealed class DefineBlockFromNodeTests
{
    private static readonly Locale En = new("en");

    /// <summary>Section [ Divider, Stack [ Heading "Original" ] ] — the stack becomes the block.</summary>
    private sealed record Fixture(PageId PageId, NodeId SectionId, NodeId DividerId, NodeId StackId, NodeId HeadingId);

    private static async Task<Fixture> ArrangePage(AuthoringTestHost host)
    {
        var siteId = await host.CreateTestSite();
        var sectionId = NodeId.New();
        var dividerId = NodeId.New();
        var stackId = NodeId.New();
        var headingId = NodeId.New();
        var pageId = await host.CreateTestPage(siteId, build: page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = sectionId,
            Children = NodeList.Of(
                new DividerNode { Id = dividerId },
                new StackNode
                {
                    Id = stackId,
                    Children = NodeList.Of(new HeadingNode
                    {
                        Id = headingId,
                        Level = 2,
                        Text = LocalizedText.Of(En, "Original"),
                    }),
                }),
        }));
        return new Fixture(pageId, sectionId, dividerId, stackId, headingId);
    }

    [Fact]
    public async Task DefineBlockFromNode_creates_the_definition_and_swaps_in_an_instance_at_the_same_position()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangePage(host);
        var blockId = BlockDefinitionId.New();

        await host.Ok(new DefineBlockFromNode(fixture.PageId, fixture.StackId, blockId, "Promo"));

        // The definition exists, named, with the stack's structure.
        var definition = host.Get<BlockLibrary>().Get(blockId);
        Assert.NotNull(definition);
        Assert.Equal("Promo", definition.Name);
        var spec = Assert.IsType<StackNode>(definition.Spec);
        var specHeading = Assert.IsType<HeadingNode>(Assert.Single(spec.Children));
        Assert.Equal("Original", specHeading.Text.Get(En));

        // ... but with fresh ids: overrides key on definition node ids, which must
        // never collide with page node ids.
        var originalIds = new[] { fixture.StackId, fixture.HeadingId };
        Assert.Empty(PageTree.Flatten(definition.Spec).Select(n => n.Id).Intersect(originalIds));

        // The page now holds a block instance where the stack was: same parent
        // (the section), same index (1, after the divider), original subtree gone.
        var tree = host.Get<PageDrafts>().Get(fixture.PageId)!.Tree;
        var section = Assert.IsType<SectionNode>(tree.Find(fixture.SectionId));
        Assert.Equal(2, section.Children.Count);
        Assert.Equal(fixture.DividerId, section.Children[0].Id);
        var instance = Assert.IsType<BlockInstanceNode>(section.Children[1]);
        Assert.Equal(blockId, instance.DefinitionId);
        Assert.Equal(0, instance.Overrides.Count);
        Assert.Null(tree.Find(fixture.StackId));
        Assert.Null(tree.Find(fixture.HeadingId));
    }

    [Fact]
    public async Task DefineBlockFromNode_from_an_unknown_node_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangePage(host);

        var error = await host.Fails(new DefineBlockFromNode(fixture.PageId, NodeId.New(), BlockDefinitionId.New(), "Promo"));

        Assert.Contains("no longer exists", error);
    }

    [Fact]
    public async Task DefineBlockFromNode_from_a_section_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangePage(host);
        var blockId = BlockDefinitionId.New();

        var error = await host.Fails(new DefineBlockFromNode(fixture.PageId, fixture.SectionId, blockId, "Promo"));

        Assert.Contains("section cannot become a block", error);
        Assert.Null(host.Get<BlockLibrary>().Get(blockId));
    }

    [Fact]
    public async Task DefineBlockFromNode_from_a_managed_column_cell_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var siteId = await host.CreateTestSite();
        var cellId = NodeId.New();
        var pageId = await host.CreateTestPage(siteId, build: page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new ColumnsNode
            {
                Id = NodeId.New(),
                Ratios = [1, 1],
                Children = NodeList.Of(new StackNode { Id = cellId }, new StackNode { Id = NodeId.New() }),
            }),
        }));

        var error = await host.Fails(new DefineBlockFromNode(pageId, cellId, BlockDefinitionId.New(), "Promo"));

        Assert.Contains("column cell cannot become a block", error);
    }

    [Fact]
    public async Task DefineBlockFromNode_from_an_existing_block_instance_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangePage(host);
        await host.Ok(new DefineBlockFromNode(fixture.PageId, fixture.StackId, BlockDefinitionId.New(), "Promo"));
        var tree = host.Get<PageDrafts>().Get(fixture.PageId)!.Tree;
        var instanceId = Assert.IsType<SectionNode>(tree.Find(fixture.SectionId)).Children[1].Id;

        var error = await host.Fails(new DefineBlockFromNode(fixture.PageId, instanceId, BlockDefinitionId.New(), "Again"));

        Assert.Contains("already a block instance", error);
    }

    [Fact]
    public async Task DefineBlockFromNode_with_empty_name_is_rejected_before_either_stream_changes()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangePage(host);
        var blockId = BlockDefinitionId.New();

        var error = await host.Fails(new DefineBlockFromNode(fixture.PageId, fixture.StackId, blockId, "  "));

        Assert.Contains("needs a name", error);
        Assert.Null(host.Get<BlockLibrary>().Get(blockId));
        Assert.NotNull(host.Get<PageDrafts>().Get(fixture.PageId)!.Tree.Find(fixture.StackId));
    }
}
