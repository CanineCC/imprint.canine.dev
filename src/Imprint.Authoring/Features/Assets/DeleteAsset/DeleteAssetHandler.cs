using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Projections;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.DeleteAsset;

public sealed class DeleteAssetHandler(
    IAggregateStore store,
    ContentUsage usage,
    IMediaStore media) : ICommandHandler<DeleteAsset>
{
    public async Task<Result> Handle(DeleteAsset cmd, CancellationToken ct)
    {
        // Cross-aggregate delete protection via the ContentUsage read model — the
        // Asset aggregate cannot see page or block streams. Accepted race: a page can
        // gain a reference in the instant after this check passes; the orphaned
        // reference renders as an empty media node, visible in the editor, never a crash.
        var pages = usage.PagesUsingAsset(cmd.AssetId);
        var blocks = usage.BlocksUsingAsset(cmd.AssetId);
        if (pages.Count > 0 || blocks.Count > 0)
        {
            return Result.Fail(
                $"This asset is still used by {pages.Count} page(s) and {blocks.Count} block(s). " +
                "Remove those references first.");
        }

        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.Delete();
        await store.Save(asset, ct);

        // Bytes go last, after the event is committed: if byte deletion fails we leak
        // files for an asset that is already truthfully deleted (harmless, sweepable);
        // the reverse order could destroy the bytes of an asset that is still live.
        await media.DeleteAll(cmd.AssetId, ct);
        return Result.Ok();
    }
}
