using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets.ProcessAssetDarkVariant;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

/// <summary>
/// Drains <see cref="AssetProcessingQueue"/> and dispatches one processing command per
/// item — <see cref="ProcessUploadedAsset"/> for a base rendition,
/// <see cref="ProcessAssetDarkVariant.ProcessAssetDarkVariant"/> for a dark variant.
/// Failures are logged and the loop keeps draining — a bad file must never starve the
/// items behind it.
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

        await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                ICommand command = item.Kind == AssetProcessingKind.DarkVariant
                    ? new ProcessAssetDarkVariant.ProcessAssetDarkVariant(item.AssetId)
                    : new ProcessUploadedAsset(item.AssetId);

                var result = await dispatcher.Dispatch(command, stoppingToken);
                if (!result.Succeeded)
                {
                    logger.LogWarning(
                        "Processing {Work} for asset {AssetId} was rejected: {Error}",
                        item.Kind, item.AssetId, result.ErrorMessage);
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
                logger.LogError(
                    failure, "Processing {Work} for asset {AssetId} crashed; continuing with the queue.",
                    item.Kind, item.AssetId);
            }
        }
    }

    /// <summary>
    /// Startup crash recovery, for free, because status is *derived from events*: an
    /// asset whose stream ends at <c>asset.uploaded</c> IS an asset whose processing
    /// never completed — no dirty flags, no requeue table, no reconciliation job. The
    /// same holds for a dark variant left at <c>asset.dark-variant-uploaded</c>.
    /// Re-enqueueing every Pending item from the (already replayed) AssetLibrary is the
    /// entire recovery story.
    /// </summary>
    public void RecoverPendingAssets()
    {
        foreach (var asset in assets.All())
        {
            if (asset.Status == AssetStatus.Pending)
            {
                queue.Enqueue(asset.Id);
            }

            if (asset.DarkStatus == DarkVariantStatus.Pending)
            {
                queue.EnqueueDarkVariant(asset.Id);
            }
        }
    }
}
