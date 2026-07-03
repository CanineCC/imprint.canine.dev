using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.SetAssetAlt;

public sealed class SetAssetAltHandler(IAggregateStore store) : ICommandHandler<SetAssetAlt>
{
    public async Task<Result> Handle(SetAssetAlt cmd, CancellationToken ct)
    {
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.SetAlt(new Locale(cmd.Locale), cmd.Alt);
        await store.Save(asset, ct);
        return Result.Ok();
    }
}
