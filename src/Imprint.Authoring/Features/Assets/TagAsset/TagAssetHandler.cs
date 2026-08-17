using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.TagAsset;

public sealed class TagAssetHandler(IAggregateStore store) : ICommandHandler<TagAsset>
{
    public async Task<Result> Handle(TagAsset cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.Tag(cmd.Tag);
        await store.Save(asset, ct);
        return Result.Ok();
    }
}
