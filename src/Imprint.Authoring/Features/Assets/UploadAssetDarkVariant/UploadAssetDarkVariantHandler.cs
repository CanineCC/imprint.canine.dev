using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UploadAssetDarkVariant;

public sealed class UploadAssetDarkVariantHandler(
    IAggregateStore store,
    IMediaStore media,
    AssetProcessingQueue queue) : ICommandHandler<UploadAssetDarkVariant>
{
    public async Task<Result> Handle(UploadAssetDarkVariant cmd, CancellationToken ct)
    {
        var darkKind = MediaContentType.KindOf(cmd.ContentType);

        // Bytes first, then the event: a dark original without an event is a harmless
        // orphan (superseded on the next upload to the same id); an event without bytes
        // would be a lie in the stream. Same ordering as the base UploadAsset.
        var storageKey = await media.SaveOriginal(cmd.AssetId, cmd.FileName, cmd.Content, ct);

        var asset = await store.Load<Asset>(cmd.AssetId.Stream, ct);
        asset.UploadDarkVariant(darkKind, storageKey, cmd.ContentType);
        await store.Save(asset, ct);

        // Enqueued only after the commit, so the worker never sees a dark variant whose
        // stream entry does not exist yet.
        queue.EnqueueDarkVariant(cmd.AssetId);
        return Result.Ok();
    }
}
