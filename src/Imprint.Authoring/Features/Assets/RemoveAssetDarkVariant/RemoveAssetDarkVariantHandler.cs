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

        // The dark bytes become orphans (the base asset still needs its own files, so a
        // blanket DeleteAll is wrong); they are swept when the asset itself is deleted.
        return Result.Ok();
    }
}
