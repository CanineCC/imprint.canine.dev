using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.Authoring.Features.Assets.UploadAsset;
using Imprint.Authoring.Projections;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Features.Assets;

public sealed class UploadAssetTests
{
    [Theory]
    [InlineData("photo.jpg", "image/jpeg", AssetKind.Image, AssetStatus.Pending)]
    [InlineData("logo.svg", "image/svg+xml", AssetKind.Vector, AssetStatus.Pending)]
    [InlineData("intro.mp4", "video/mp4", AssetKind.Video, AssetStatus.Pending)]
    [InlineData("brochure.pdf", "application/pdf", AssetKind.File, AssetStatus.Ready)]
    public async Task UploadAsset_derives_the_kind_saves_the_original_and_enqueues(
        string fileName, string contentType, AssetKind expectedKind, AssetStatus expectedStatus)
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();

        await host.Ok(SliceTestHelpers.NewUpload(assetId, fileName, contentType, byteSize: 2048));

        // Read-model effect: the asset panel sees it immediately.
        var asset = host.Get<AssetLibrary>().Get(assetId);
        Assert.NotNull(asset);
        Assert.Equal(expectedKind, asset.Kind);
        Assert.Equal(expectedStatus, asset.Status);
        Assert.Equal(fileName, asset.FileName);
        Assert.Equal(2048, asset.ByteSize);

        // The original bytes were streamed to the media store under the recorded key.
        var media = (InMemoryMediaStore)host.Get<IMediaStore>();
        Assert.Equal(2048, media.Files[asset.OriginalStorageKey].Length);

        // And the id is on the processing queue as a Base item (File kind included —
        // the processing handler no-ops for it; one queue, one rule).
        var queue = host.Get<AssetProcessingQueue>();
        Assert.True(queue.Reader.TryRead(out var queued));
        Assert.Equal(new AssetProcessingItem(assetId, AssetProcessingKind.Base), queued);
        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task UploadAsset_svg_wins_over_the_image_family()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();

        await host.Ok(SliceTestHelpers.NewUpload(assetId, "logo.svg", "IMAGE/SVG+XML"));

        Assert.Equal(AssetKind.Vector, host.Get<AssetLibrary>().Get(assetId)!.Kind);
    }

    [Fact]
    public async Task UploadAsset_with_empty_file_name_fails_validation()
    {
        await using var host = SliceTestHelpers.NewAssetHost();

        var error = await host.Fails(SliceTestHelpers.NewUpload(AssetId.New(), "  ", "image/png"));

        Assert.Contains("needs a file name", error);
    }

    [Fact]
    public async Task UploadAsset_with_malformed_content_type_fails_validation()
    {
        await using var host = SliceTestHelpers.NewAssetHost();

        var error = await host.Fails(SliceTestHelpers.NewUpload(AssetId.New(), "photo.jpg", "jpeg"));

        Assert.Contains("not a media type", error);
    }

    [Fact]
    public async Task UploadAsset_with_zero_bytes_fails_validation()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var command = new UploadAsset(AssetId.New(), "photo.jpg", "image/png", 0, new MemoryStream());

        var error = await host.Fails(command);

        Assert.Contains("cannot be empty", error);
        Assert.Empty(host.Get<AssetLibrary>().All());
    }
}
