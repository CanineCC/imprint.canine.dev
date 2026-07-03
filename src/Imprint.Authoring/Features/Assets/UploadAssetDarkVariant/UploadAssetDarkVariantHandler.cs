using Imprint.Authoring.Domain.Assets;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UploadAssetDarkVariant;

public sealed class UploadAssetDarkVariantHandler(
    IAggregateStore store,
    IMediaStore media) : ICommandHandler<UploadAssetDarkVariant>
{
    public async Task<Result> Handle(UploadAssetDarkVariant cmd, CancellationToken ct)
    {
        // Bytes first, then the event (same ordering rationale as UploadAsset). The
        // "dark-" prefix keeps the dark original from colliding with the base original
        // under the asset's storage namespace.
        var storageKey = await media.SaveOriginal(cmd.AssetId, $"dark-{cmd.FileName}", cmd.Content, ct);

        // The aggregate enforces the invariants (image/vector only, base already
        // processed, kind match) and re-enters Pending on a replacing upload.
        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.UploadDarkVariant(storageKey, cmd.ContentType);
        await store.Save(asset, ct);

        // Derivative generation for the dark original is the processing pipeline's job
        // (ProcessAssetDarkVariant), enqueued the same way an upload is — see
        // docs/proposals/theme-media-and-widget-approval.md §Part 1.
        return Result.Ok();
    }
}
