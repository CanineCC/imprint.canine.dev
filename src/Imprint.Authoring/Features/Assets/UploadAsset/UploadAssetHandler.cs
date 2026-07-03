using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Features.Assets.ProcessUploadedAsset;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Assets.UploadAsset;

public sealed class UploadAssetHandler(
    IAggregateStore store,
    IMediaStore media,
    AssetProcessingQueue queue) : ICommandHandler<UploadAsset>
{
    public async Task<Result> Handle(UploadAsset cmd, CancellationToken ct)
    {
        var kind = KindOf(cmd.ContentType);

        // Bytes first, then the event: an original without an event is an orphaned
        // file (invisible, cleaned by the next upload to the same id); an event
        // without bytes would be a lie in the stream.
        var storageKey = await media.SaveOriginal(cmd.AssetId, cmd.FileName, cmd.Content, ct);

        var asset = Asset.Upload(cmd.AssetId, cmd.FileName, cmd.ContentType, kind, cmd.ByteSize, storageKey);
        await store.Save(asset, ct);

        // Enqueued only after the commit: the worker must never see an asset whose
        // stream does not exist yet. File-kind assets take the same path and no-op in
        // the processing handler — one queue, one rule.
        queue.Enqueue(cmd.AssetId);
        return Result.Ok();
    }

    // The kind decides the processing pipeline; svg must be tested before the image/*
    // family because it matches both.
    private static AssetKind KindOf(string contentType)
    {
        var type = contentType.Trim().ToLowerInvariant();
        return type switch
        {
            "image/svg+xml" => AssetKind.Vector,
            _ when type.StartsWith("image/", StringComparison.Ordinal) => AssetKind.Image,
            _ when type.StartsWith("video/", StringComparison.Ordinal) => AssetKind.Video,
            _ => AssetKind.File,
        };
    }
}
