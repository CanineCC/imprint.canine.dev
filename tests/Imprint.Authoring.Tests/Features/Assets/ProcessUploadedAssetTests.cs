using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.Authoring.Projections;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Features.Assets;

public sealed class ProcessUploadedAssetTests
{
    [Fact]
    public async Task Image_processing_generates_variants_and_becomes_Ready()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));

        await host.Ok(new ProcessUploadedAsset(assetId));

        var asset = host.Get<AssetLibrary>().Get(assetId)!;
        Assert.Equal(AssetStatus.Ready, asset.Status);
        Assert.Equal([480, 960], asset.Variants.Select(v => v.Width));
    }

    [Fact]
    public async Task Svg_processing_records_the_sanitized_key_and_becomes_Ready()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "logo.svg", "image/svg+xml"));

        await host.Ok(new ProcessUploadedAsset(assetId));

        var asset = host.Get<AssetLibrary>().Get(assetId)!;
        Assert.Equal(AssetStatus.Ready, asset.Status);
        Assert.Equal($"derived/{assetId.Compact}/clean.svg", asset.DerivedStorageKey);
    }

    [Fact]
    public async Task Video_processing_records_the_webm_key_and_becomes_Ready()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "intro.mp4", "video/mp4"));

        await host.Ok(new ProcessUploadedAsset(assetId));

        var asset = host.Get<AssetLibrary>().Get(assetId)!;
        Assert.Equal(AssetStatus.Ready, asset.Status);
        Assert.Equal($"derived/{assetId.Compact}/video.webm", asset.DerivedStorageKey);
    }

    [Fact]
    public async Task File_kind_is_a_no_op_that_appends_nothing()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "brochure.pdf", "application/pdf"));

        await host.Ok(new ProcessUploadedAsset(assetId));

        Assert.Equal(AssetStatus.Ready, host.Get<AssetLibrary>().Get(assetId)!.Status);
        // Still just the upload event: nothing was processed, so nothing was recorded.
        var stream = await host.Store.ReadStream(assetId.Stream);
        Assert.Single(stream);
    }

    [Fact]
    public async Task Processor_exception_is_recorded_as_Failed_with_the_reason()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));
        ((FakeMediaProcessor)host.Get<IMediaProcessor>()).FailNext = true;

        // The command SUCCEEDS: recording a failure as a fact is the happy path of
        // this slice — the pipeline never crashes over a bad file.
        await host.Ok(new ProcessUploadedAsset(assetId));

        var asset = host.Get<AssetLibrary>().Get(assetId)!;
        Assert.Equal(AssetStatus.Failed, asset.Status);
        Assert.Equal("Simulated processing failure", asset.StatusReason);
    }

    [Fact]
    public async Task Video_without_transcoder_is_recorded_as_ReadyDegraded_with_the_reason()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "intro.mp4", "video/mp4"));
        ((FakeMediaProcessor)host.Get<IMediaProcessor>()).VideoAvailable = false;

        await host.Ok(new ProcessUploadedAsset(assetId));

        var asset = host.Get<AssetLibrary>().Get(assetId)!;
        Assert.Equal(AssetStatus.ReadyDegraded, asset.Status);
        Assert.Equal("ffmpeg is not installed (fake)", asset.StatusReason);
    }

    [Fact]
    public async Task Reprocessing_an_already_processed_asset_is_rejected_by_the_aggregate()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));
        await host.Ok(new ProcessUploadedAsset(assetId));

        var error = await host.Fails(new ProcessUploadedAsset(assetId));

        Assert.Contains("not waiting for processing", error);
    }
}
