using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Assets.DeleteAsset;
using Imprint.Authoring.Projections;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Features.Assets;

public sealed class DeleteAssetTests
{
    [Fact]
    public async Task DeleteAsset_unused_removes_it_from_the_library_and_the_media_store()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        await host.Ok(new DeleteAsset(assetId));

        Assert.Null(host.Get<AssetLibrary>().Get(assetId));
        // Bytes go last, but they do go: nothing of this asset survives on disk.
        var media = (InMemoryMediaStore)host.Get<IMediaStore>();
        Assert.DoesNotContain(media.Files.Keys, key => key.Contains(assetId.Compact, StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteAsset_referenced_by_a_page_is_rejected_with_usage_counts()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        var siteId = await host.CreateTestSite();
        await host.CreateTestPage(siteId, build: page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new ImageNode { Id = NodeId.New(), AssetId = assetId }),
        }));

        var error = await host.Fails(new DeleteAsset(assetId));

        Assert.Contains("1 page(s)", error);
        Assert.Contains("0 block(s)", error);
        Assert.NotNull(host.Get<AssetLibrary>().Get(assetId));
    }

    [Fact]
    public async Task DeleteAsset_referenced_by_a_block_definition_is_rejected_with_usage_counts()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));
        await host.CreateTestBlock("Promo image", new ImageNode { Id = NodeId.New(), AssetId = assetId });

        var error = await host.Fails(new DeleteAsset(assetId));

        Assert.Contains("0 page(s)", error);
        Assert.Contains("1 block(s)", error);
        Assert.NotNull(host.Get<AssetLibrary>().Get(assetId));
    }

    [Fact]
    public async Task DeleteAsset_succeeds_after_the_last_reference_is_removed()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        var imageNodeId = NodeId.New();
        var siteId = await host.CreateTestSite();
        var pageId = await host.CreateTestPage(siteId, build: page => page.AddNode(NodeId.Root, 0, new SectionNode
        {
            Id = NodeId.New(),
            Children = NodeList.Of(new ImageNode { Id = imageNodeId, AssetId = assetId }),
        }));
        await host.Fails(new DeleteAsset(assetId));

        await host.MutatePage(pageId, page => page.RemoveNode(imageNodeId));

        await host.Ok(new DeleteAsset(assetId));
        Assert.Null(host.Get<AssetLibrary>().Get(assetId));
    }
}
