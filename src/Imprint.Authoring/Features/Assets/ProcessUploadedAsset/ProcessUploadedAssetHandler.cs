using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

public sealed class ProcessUploadedAssetHandler(
    IAggregateStore store,
    IMediaProcessor processor) : ICommandHandler<ProcessUploadedAsset>
{
    public async Task<Result> Handle(ProcessUploadedAsset cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);

        // Plain files have no derivative pipeline: they are Ready the moment they are
        // uploaded, so there is nothing to run and nothing to record.
        if (asset.Kind == AssetKind.File)
        {
            return Result.Ok();
        }

        Action<Asset> record;
        try
        {
            record = await RunProcessor(asset, ct);
        }
        catch (OperationCanceledException)
        {
            throw; // Shutdown, not a media problem — never record it against the asset.
        }
        catch (Exception failure)
        {
            // The processor is untrusted infrastructure (docs/domain-model.md §3): ANY
            // exception it throws becomes a recorded fact on the aggregate, and the
            // processing pipeline never crashes over one bad file.
            record = a => a.FailProcessing(failure.Message);
        }

        // Recording happens outside the try: a DomainException here (e.g. the asset
        // was already processed by a duplicate enqueue) is an aggregate verdict for
        // the dispatcher, not a media failure to bury.
        record(asset);
        await store.Save(asset, ct);
        return Result.Ok();
    }

    private async Task<Action<Asset>> RunProcessor(Asset asset, CancellationToken ct)
    {
        switch (asset.Kind)
        {
            case AssetKind.Image:
            {
                var variants = await processor.GenerateImageVariants(asset.Id, asset.OriginalStorageKey, ct);
                return a => a.CompleteImageVariants(variants);
            }

            case AssetKind.Vector:
            {
                var (storageKey, removedNodes) = await processor.SanitizeSvg(asset.Id, asset.OriginalStorageKey, ct);
                return a => a.CompleteSvgSanitize(storageKey, removedNodes);
            }

            case AssetKind.Video:
            {
                var transcoded = await processor.TranscodeToWebM(asset.Id, asset.OriginalStorageKey, ct);
                return transcoded is { } webm
                    ? a => a.CompleteVideoTranscode(webm.StorageKey, webm.ByteSize)
                    // Null means unavailable (no ffmpeg), not broken: the asset stays
                    // publishable as its original, visibly degraded in the editor.
                    : a => a.SkipProcessing(processor.VideoUnavailableReason ?? "Video transcoding is unavailable.");
            }

            default:
                throw new InvalidOperationException($"Asset kind {asset.Kind} has no processing pipeline.");
        }
    }
}
