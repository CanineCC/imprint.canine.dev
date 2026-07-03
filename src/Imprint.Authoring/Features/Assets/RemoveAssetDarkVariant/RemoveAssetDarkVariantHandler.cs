using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.RemoveAssetDarkVariant;

public sealed class RemoveAssetDarkVariantHandler(IAggregateStore store) : ICommandHandler<RemoveAssetDarkVariant>
{
    public async Task<Result> Handle(RemoveAssetDarkVariant cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.RemoveDarkVariant();
        await store.Save(asset, ct);

        // The dark original and derivatives are deliberately left in the media store.
        // IMediaStore only offers DeleteAll, which removes *every* file of the asset —
        // including the base rendition that is still live — so it cannot serve a
        // dark-only removal, and a targeted delete would need a new port method that
        // ripples beyond this slice. The orphaned dark files are harmless and sweepable,
        // the same tolerance UploadAsset already accepts for a superseded original.
        return Result.Ok();
    }
}
