using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Blocks.DeleteBlock;
using Imprint.Authoring.Features.Blocks.RenameBlock;
using Imprint.Authoring.Projections;

namespace Imprint.Authoring.Tests.Features.Blocks;

public sealed class RenameAndDeleteBlockTests
{
    private static readonly Locale En = new("en");

    private static StackNode PromoSpec() => new()
    {
        Id = NodeId.New(),
        Children = NodeList.Of(new HeadingNode
        {
            Id = NodeId.New(),
            Level = 2,
            Text = LocalizedText.Of(En, "Promo"),
        }),
    };

    [Fact]
    public async Task RenameBlock_happy_path_updates_BlockLibrary()
    {
        await using var host = new AuthoringTestHost();
        var blockId = await host.CreateTestBlock("Promo", PromoSpec());

        await host.Ok(new RenameBlock(blockId, "Hero promo"));

        Assert.Equal("Hero promo", host.Get<BlockLibrary>().Get(blockId)!.Name);
    }

    [Fact]
    public async Task RenameBlock_with_empty_name_is_rejected()
    {
        await using var host = new AuthoringTestHost();
        var blockId = await host.CreateTestBlock("Promo", PromoSpec());

        var error = await host.Fails(new RenameBlock(blockId, "  "));

        Assert.Contains("needs a name", error);
        Assert.Equal("Promo", host.Get<BlockLibrary>().Get(blockId)!.Name);
    }

    [Fact]
    public async Task DeleteBlock_without_instances_removes_it_from_BlockLibrary()
    {
        await using var host = new AuthoringTestHost();
        var blockId = await host.CreateTestBlock("Promo", PromoSpec());

        await host.Ok(new DeleteBlock(blockId));

        Assert.Null(host.Get<BlockLibrary>().Get(blockId));
    }

    [Fact]
    public async Task DeleteBlock_with_live_instances_is_rejected_with_counts()
    {
        await using var host = new AuthoringTestHost();
        var blockId = await host.CreateTestBlock("Promo", PromoSpec());
        var siteId = await host.CreateTestSite();
        await host.CreateTestPage(siteId, "page-one", "One", page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(
                new BlockInstanceNode { Id = NodeId.New(), DefinitionId = blockId },
                new BlockInstanceNode { Id = NodeId.New(), DefinitionId = blockId }),
        }));
        await host.CreateTestPage(siteId, "page-two", "Two", page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new BlockInstanceNode { Id = NodeId.New(), DefinitionId = blockId }),
        }));

        var error = await host.Fails(new DeleteBlock(blockId));

        Assert.Contains("3 time(s)", error);
        Assert.Contains("2 page(s)", error);
        Assert.NotNull(host.Get<BlockLibrary>().Get(blockId));
    }

    [Fact]
    public async Task DeleteBlock_succeeds_after_the_last_instance_is_removed()
    {
        await using var host = new AuthoringTestHost();
        var blockId = await host.CreateTestBlock("Promo", PromoSpec());
        var siteId = await host.CreateTestSite();
        var instanceId = NodeId.New();
        var pageId = await host.CreateTestPage(siteId, build: page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new BlockInstanceNode { Id = instanceId, DefinitionId = blockId }),
        }));
        await host.Fails(new DeleteBlock(blockId));

        await host.MutatePage(pageId, page => page.RemoveNode(instanceId));

        await host.Ok(new DeleteBlock(blockId));
        Assert.Null(host.Get<BlockLibrary>().Get(blockId));
    }
}
