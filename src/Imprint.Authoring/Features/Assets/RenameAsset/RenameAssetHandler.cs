using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.RenameAsset;

public sealed class RenameAssetHandler(IAggregateStore store) : ICommandHandler<RenameAsset>
{
    public async Task<Result> Handle(RenameAsset cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.Rename(cmd.Name);
        await store.Save(asset, ct);
        return Result.Ok();
    }
}
