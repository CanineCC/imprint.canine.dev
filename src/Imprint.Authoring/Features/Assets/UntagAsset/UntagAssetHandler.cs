using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UntagAsset;

public sealed class UntagAssetHandler(IAggregateStore store) : ICommandHandler<UntagAsset>
{
    public async Task<Result> Handle(UntagAsset cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.Untag(cmd.Tag);
        await store.Save(asset, ct);
        return Result.Ok();
    }
}
