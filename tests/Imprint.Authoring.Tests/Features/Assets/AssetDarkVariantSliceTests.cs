using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets;
using Imprint.Authoring.Features.Assets.ProcessAssetDarkVariant;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.Authoring.Features.Assets.RemoveAssetDarkVariant;
using Imprint.Authoring.Features.Assets.UploadAssetDarkVariant;
using Imprint.Authoring.Projections;
using Imprint.TestKit;

namespace Imprint.Authoring.Tests.Features.Assets;

/// <summary>
/// The dark-variant slices end to end through the real store, dispatcher and
/// projections: upload → enqueue → process, plus the fail, replace and remove paths.
/// </summary>
public sealed class AssetDarkVariantSliceTests
{
    private static UploadAssetDarkVariant NewDarkUpload(
        AssetId id, string fileName, string contentType, int byteSize = 1024) =>
        new(id, fileName, contentType, byteSize, new MemoryStream(new byte[byteSize]));

    /// <summary>Uploads a base asset and drives it to Ready, draining the base work item
    /// the worker would have consumed so the queue reflects a settled base.</summary>
    private static async Task<AssetId> ReadyBaseAsset(AuthoringTestHost host, string fileName, string contentType)
    {
        var id = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(id, fileName, contentType));
        await host.Ok(new ProcessUploadedAsset(id));
        while (host.Get<AssetProcessingQueue>().Reader.TryRead(out _))
        {
            // Discard the base enqueue: this test dispatches processing directly, so the
            // real worker's drain never ran.
        }

        return id;
    }

    [Fact]
    public async Task Dark_image_variant_uploads_enqueues_and_processes_to_ready()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");

        await host.Ok(NewDarkUpload(id, "logo-dark.png", "image/png", byteSize: 2048));

        // Upload effects: the dark original is stored and the variant is Pending.
        var afterUpload = host.Get<AssetLibrary>().Get(id)!;
        Assert.Equal(DarkVariantStatus.Pending, afterUpload.DarkStatus);
        Assert.False(afterUpload.HasDarkVariant);
        var media = (InMemoryMediaStore)host.Get<IMediaStore>();
        Assert.Equal(2048, media.Files[afterUpload.DarkOriginalStorageKey!].Length);

        // Enqueued as a DarkVariant work item.
        var queue = host.Get<AssetProcessingQueue>();
        Assert.True(queue.Reader.TryRead(out var queued));
        Assert.Equal(new AssetProcessingItem(id, AssetProcessingKind.DarkVariant), queued);

        await host.Ok(new ProcessAssetDarkVariant(id));

        var ready = host.Get<AssetLibrary>().Get(id)!;
        Assert.Equal(DarkVariantStatus.Ready, ready.DarkStatus);
        Assert.True(ready.HasDarkVariant);
        Assert.Equal([480, 960], ready.DarkVariants.Select(v => v.Width));
        // The base rendition is untouched.
        Assert.Equal(AssetStatus.Ready, ready.Status);
        Assert.Equal([480, 960], ready.Variants.Select(v => v.Width));
    }

    [Fact]
    public async Task Dark_svg_variant_processes_to_ready_with_a_sanitized_key()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.svg", "image/svg+xml");

        await host.Ok(NewDarkUpload(id, "logo-dark.svg", "image/svg+xml"));
        await host.Ok(new ProcessAssetDarkVariant(id));

        var ready = host.Get<AssetLibrary>().Get(id)!;
        Assert.Equal(DarkVariantStatus.Ready, ready.DarkStatus);
        // The dark rendition lives under a DISTINCT key so it never overwrites the base
        // sanitized SVG (both share the asset's derived/ folder and the same processing run).
        Assert.Equal($"derived/{id.Compact}/dark-clean.svg", ready.DarkDerivedStorageKey);
        Assert.NotEqual(ready.DerivedStorageKey, ready.DarkDerivedStorageKey);
    }

    [Fact]
    public async Task Dark_variant_upload_on_a_video_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "intro.mp4", "video/mp4");

        var error = await host.Fails(NewDarkUpload(id, "intro-dark.mp4", "video/mp4"));

        Assert.Contains("only images and SVGs", error);
        Assert.Equal(DarkVariantStatus.None, host.Get<AssetLibrary>().Get(id)!.DarkStatus);
    }

    [Fact]
    public async Task Dark_variant_upload_with_a_mismatched_kind_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");

        // An SVG dark file onto a raster image: the slice derives Vector, the aggregate
        // rejects the kind mismatch.
        var error = await host.Fails(NewDarkUpload(id, "logo-dark.svg", "image/svg+xml"));

        Assert.Contains("same kind", error);
    }

    [Fact]
    public async Task Uploading_a_second_dark_variant_re_enters_pending_and_reprocesses()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");
        await host.Ok(NewDarkUpload(id, "logo-dark.png", "image/png"));
        await host.Ok(new ProcessAssetDarkVariant(id));
        Assert.Equal(DarkVariantStatus.Ready, host.Get<AssetLibrary>().Get(id)!.DarkStatus);

        await host.Ok(NewDarkUpload(id, "logo-dark-v2.png", "image/png", byteSize: 4096));

        var replaced = host.Get<AssetLibrary>().Get(id)!;
        Assert.Equal(DarkVariantStatus.Pending, replaced.DarkStatus);
        Assert.Empty(replaced.DarkVariants);

        await host.Ok(new ProcessAssetDarkVariant(id));
        Assert.Equal(DarkVariantStatus.Ready, host.Get<AssetLibrary>().Get(id)!.DarkStatus);
    }

    [Fact]
    public async Task Dark_processing_failure_is_recorded_and_reverts_to_neutral()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");
        await host.Ok(NewDarkUpload(id, "logo-dark.png", "image/png"));
        ((FakeMediaProcessor)host.Get<IMediaProcessor>()).FailNext = true;

        // The command SUCCEEDS: dropping the variant is the graceful outcome, the
        // pipeline never crashes over a bad dark file.
        await host.Ok(new ProcessAssetDarkVariant(id));

        var asset = host.Get<AssetLibrary>().Get(id)!;
        Assert.Equal(DarkVariantStatus.None, asset.DarkStatus);
        Assert.Null(asset.DarkOriginalStorageKey);
        // Base asset still usable and neutral.
        Assert.Equal(AssetStatus.Ready, asset.Status);
    }

    [Fact]
    public async Task Reprocessing_a_ready_dark_variant_is_a_no_op_that_appends_nothing()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");
        await host.Ok(NewDarkUpload(id, "logo-dark.png", "image/png"));
        await host.Ok(new ProcessAssetDarkVariant(id));
        var lengthAfterProcessing = (await host.Store.ReadStream(id.Stream)).Count;

        // A stale duplicate enqueue: nothing to run, nothing to record.
        await host.Ok(new ProcessAssetDarkVariant(id));

        Assert.Equal(DarkVariantStatus.Ready, host.Get<AssetLibrary>().Get(id)!.DarkStatus);
        Assert.Equal(lengthAfterProcessing, (await host.Store.ReadStream(id.Stream)).Count);
    }

    [Fact]
    public async Task RemoveAssetDarkVariant_reverts_the_asset_to_neutral()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");
        await host.Ok(NewDarkUpload(id, "logo-dark.png", "image/png"));
        await host.Ok(new ProcessAssetDarkVariant(id));

        await host.Ok(new RemoveAssetDarkVariant(id));

        var asset = host.Get<AssetLibrary>().Get(id)!;
        Assert.Equal(DarkVariantStatus.None, asset.DarkStatus);
        Assert.False(asset.HasDarkVariant);
        Assert.Empty(asset.DarkVariants);
        // The base asset survives the removal.
        Assert.Equal(AssetStatus.Ready, asset.Status);
    }

    [Fact]
    public async Task RemoveAssetDarkVariant_when_none_exists_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = await ReadyBaseAsset(host, "logo.png", "image/png");

        var error = await host.Fails(new RemoveAssetDarkVariant(id));

        Assert.Contains("no dark-mode version", error);
    }

    [Fact]
    public async Task Dark_variant_upload_before_the_base_is_ready_is_rejected()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var id = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(id, "logo.png", "image/png")); // Pending, not processed

        var error = await host.Fails(NewDarkUpload(id, "logo-dark.png", "image/png"));

        Assert.Contains("not ready yet", error);
    }
}
