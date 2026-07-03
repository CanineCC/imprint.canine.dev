using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

/// <summary>
/// Drains <see cref="AssetProcessingQueue"/> and dispatches one
/// <see cref="ProcessUploadedAsset"/> per id. Failures are logged and the loop keeps
/// draining — a bad file must never starve the assets behind it.
/// </summary>
public sealed class AssetProcessingWorker(
    AssetProcessingQueue queue,
    AssetLibrary assets,
    ICommandDispatcher dispatcher,
    ILogger<AssetProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RecoverPendingAssets();

        await foreach (var assetId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var result = await dispatcher.Dispatch(new ProcessUploadedAsset(assetId), stoppingToken);
                if (!result.Succeeded)
                {
                    logger.LogWarning(
                        "Processing asset {AssetId} was rejected: {Error}", assetId, result.ErrorMessage);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw; // Orderly shutdown.
            }
            catch (Exception failure)
            {
                // Anything the dispatcher did not translate (a missing stream, a
                // store hiccup). Log and keep draining — the queue must outlive any
                // single item.
                logger.LogError(failure, "Processing asset {AssetId} crashed; continuing with the queue.", assetId);
            }
        }
    }

    /// <summary>
    /// Startup crash recovery, for free, because status is *derived from events*: an
    /// asset whose stream ends at <c>asset.uploaded</c> IS an asset whose processing
    /// never completed — no dirty flags, no requeue table, no reconciliation job.
    /// Re-enqueueing every Pending asset from the (already replayed) AssetLibrary is
    /// the entire recovery story.
    /// </summary>
    public void RecoverPendingAssets()
    {
        foreach (var asset in assets.All().Where(asset => asset.Status == AssetStatus.Pending))
        {
            queue.Enqueue(asset.Id);
        }
    }
}
