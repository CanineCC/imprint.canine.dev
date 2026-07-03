using Imprint.Authoring.Domain;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.Authoring.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Imprint.Authoring.Tests.Features.Assets;

/// <summary>
/// The worker's startup-recovery logic, driven directly (construct + invoke) rather
/// than by racing the drain loop — the loop is a two-liner over the dispatcher, but
/// the recovery decision is behavior worth pinning down.
/// </summary>
public sealed class AssetProcessingWorkerTests
{
    [Fact]
    public async Task Startup_recovery_enqueues_exactly_the_pending_assets()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var pending = AssetId.New();
        var processed = AssetId.New();
        var file = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(pending, "waiting.jpg", "image/jpeg"));
        await host.Ok(SliceTestHelpers.NewUpload(processed, "done.jpg", "image/jpeg"));
        await host.Ok(new ProcessUploadedAsset(processed)); // → Ready
        await host.Ok(SliceTestHelpers.NewUpload(file, "doc.pdf", "application/pdf")); // Ready on upload

        // Simulate a restart: a fresh queue (the old one's contents died with the
        // process) against the already-replayed read models.
        var freshQueue = new AssetProcessingQueue();
        var worker = new AssetProcessingWorker(
            freshQueue, host.Get<AssetLibrary>(), host.Dispatcher, NullLogger<AssetProcessingWorker>.Instance);

        worker.RecoverPendingAssets();

        Assert.True(freshQueue.Reader.TryRead(out var recovered));
        Assert.Equal(pending, recovered);
        Assert.False(freshQueue.Reader.TryRead(out _), "only the Pending asset should be re-enqueued");
    }

    [Fact]
    public async Task Startup_recovery_with_nothing_pending_enqueues_nothing()
    {
        await using var host = SliceTestHelpers.NewAssetHost();
        var assetId = AssetId.New();
        await host.Ok(SliceTestHelpers.NewUpload(assetId, "photo.jpg", "image/jpeg"));
        await host.Ok(new ProcessUploadedAsset(assetId));

        var freshQueue = new AssetProcessingQueue();
        var worker = new AssetProcessingWorker(
            freshQueue, host.Get<AssetLibrary>(), host.Dispatcher, NullLogger<AssetProcessingWorker>.Instance);

        worker.RecoverPendingAssets();

        Assert.False(freshQueue.Reader.TryRead(out _));
    }
}
