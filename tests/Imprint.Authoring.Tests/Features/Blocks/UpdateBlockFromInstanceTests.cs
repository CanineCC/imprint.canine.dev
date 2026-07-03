using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Blocks.DefineBlockFromNode;
using Imprint.Authoring.Features.Blocks.UpdateBlockFromInstance;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Blocks;

public sealed class UpdateBlockFromInstanceTests
{
    private static readonly Locale En = new("en");

    private sealed record Fixture(PageId PageId, BlockDefinitionId BlockId, NodeId InstanceId, NodeId DefinitionHeadingId, NodeId DividerId);

    /// <summary>A page whose section holds [ Divider, BlockInstance(of Stack[Heading "Original"]) ].</summary>
    private static async Task<Fixture> ArrangeInstance(AuthoringTestHost host)
    {
        var siteId = await host.CreateTestSite();
        var sectionId = NodeId.New();
        var stackId = NodeId.New();
        var dividerId = NodeId.New();
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
                        Id = NodeId.New(),
                        Level = 2,
                        Text = LocalizedText.Of(En, "Original"),
                    }),
                }),
        }));

        var blockId = BlockDefinitionId.New();
        await host.Ok(new DefineBlockFromNode(pageId, stackId, blockId, "Promo"));

        var tree = host.Get<PageDrafts>().Get(pageId)!.Tree;
        var instanceId = ((SectionNode)tree.Find(sectionId)!).Children[1].Id;
        var definitionHeadingId = PageTree
            .Flatten(host.Get<BlockLibrary>().Get(blockId)!.Spec)
            .OfType<HeadingNode>()
            .Single()
            .Id;
        return new Fixture(pageId, blockId, instanceId, definitionHeadingId, dividerId);
    }

    [Fact]
    public async Task UpdateBlockFromInstance_bakes_overrides_into_the_definition_and_clears_them()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangeInstance(host);
        await host.MutatePage(fixture.PageId, page =>
            page.SetBlockOverride(fixture.InstanceId, fixture.DefinitionHeadingId, "text", En, "Overridden"));

        await host.Ok(new UpdateBlockFromInstance(fixture.PageId, fixture.InstanceId));

        // The definition now carries the instance's resolved content...
        var definition = host.Get<BlockLibrary>().Get(fixture.BlockId)!;
        var heading = PageTree.Flatten(definition.Spec).OfType<HeadingNode>().Single();
        Assert.Equal("Overridden", heading.Text.Get(En));
        // ... keeping the definition's node ids stable (overrides on other instances
        // still resolve against the same ids).
        Assert.Equal(fixture.DefinitionHeadingId, heading.Id);

        // And the pushing instance's overrides are cleared — baked in, they would
        // otherwise shadow every future edit of the definition.
        var tree = host.Get<PageDrafts>().Get(fixture.PageId)!.Tree;
        var instance = Assert.IsType<BlockInstanceNode>(tree.Find(fixture.InstanceId));
        Assert.Equal(0, instance.Overrides.Count);
    }

    [Fact]
    public async Task UpdateBlockFromInstance_with_no_overrides_is_a_no_op()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangeInstance(host);

        await host.Ok(new UpdateBlockFromInstance(fixture.PageId, fixture.InstanceId));

        // Nothing to push: the definition stream still holds only block.defined.
        var stream = await host.Store.ReadStream(fixture.BlockId.Stream);
        Assert.Single(stream);
    }

    [Fact]
    public async Task UpdateBlockFromInstance_on_an_unknown_node_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangeInstance(host);

        var error = await host.Fails(new UpdateBlockFromInstance(fixture.PageId, NodeId.New()));

        Assert.Contains("no longer exists", error);
    }

    [Fact]
    public async Task UpdateBlockFromInstance_on_a_node_that_is_not_an_instance_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var fixture = await ArrangeInstance(host);

        var error = await host.Fails(new UpdateBlockFromInstance(fixture.PageId, fixture.DividerId));

        Assert.Contains("not a block instance", error);
    }
}
