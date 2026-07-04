using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.ProcessAssetDarkVariant;

public sealed class ProcessAssetDarkVariantHandler(
    IAggregateStore store,
    IMediaProcessor processor) : ICommandHandler<ProcessAssetDarkVariant>
{
    public async Task<Result> Handle(ProcessAssetDarkVariant cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);

        // A dark variant is processed exactly once, while Pending — the only state that
        // carries a stored original. Any other state (already Ready, failed, removed, or
        // a stale duplicate enqueue) has no original and nothing to run: a no-op, never
        // a crash. This mirrors the base pipeline's File short-circuit.
        if (asset.DarkStatus != DarkVariantStatus.Pending || asset.DarkOriginalStorageKey is not { } darkOriginal)
        {
            return Result.Ok();
        }

        Action<Asset> record;
        try
        {
            record = await RunProcessor(asset, darkOriginal, ct);
        }
        catch (OperationCanceledException)
        {
            throw; // Shutdown, not a media problem — never record it against the asset.
        }
        catch (Exception failure)
        {
            // The processor is untrusted infrastructure: ANY exception it throws becomes
            // a recorded fact (the variant is dropped, the base stays neutral/usable),
            // and the processing pipeline never crashes over one bad file.
            record = a => a.FailDarkVariant(failure.Message);
        }

        record(asset);
        await store.Save(asset, ct);
        return Result.Ok();
    }

    private async Task<Action<Asset>> RunProcessor(Asset asset, string darkOriginal, CancellationToken ct)
    {
        switch (asset.Kind)
        {
            case AssetKind.Image:
            {
                var variants = await processor.GenerateImageVariants(asset.Id, darkOriginal, dark: true, ct: ct);
                return a => a.CompleteDarkImageVariants(variants);
            }

            case AssetKind.Vector:
            {
                var (storageKey, removedNodes) = await processor.SanitizeSvg(asset.Id, darkOriginal, dark: true, ct: ct);
                return a => a.CompleteDarkSvg(storageKey, removedNodes);
            }

            default:
                // Unreachable: UploadDarkVariant only admits Image/Vector, so a dark
                // variant never exists on another kind.
                throw new InvalidOperationException($"Asset kind {asset.Kind} has no dark-variant processing pipeline.");
        }
    }
}
