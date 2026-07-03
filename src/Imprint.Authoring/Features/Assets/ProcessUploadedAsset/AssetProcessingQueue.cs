using System.Threading.Channels;
using Imprint.Authoring.Domain;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

/// <summary>
/// The in-process hand-off between upload and derivative processing: an unbounded
/// <see cref="Channel{T}"/> of <see cref="AssetProcessingItem"/>s. Unbounded is
/// deliberate — items are tiny, and an upload must never block behind a slow transcode.
/// The queue carries no state worth persisting: whatever is lost in a crash is
/// re-derived at startup from asset status (see <see cref="AssetProcessingWorker"/>).
/// </summary>
public sealed class AssetProcessingQueue
{
    private readonly Channel<AssetProcessingItem> _queue = Channel.CreateUnbounded<AssetProcessingItem>();

    /// <summary>Enqueues an asset's base rendition for processing.</summary>
    public void Enqueue(AssetId assetId) =>
        _queue.Writer.TryWrite(new AssetProcessingItem(assetId, AssetProcessingKind.Base));

    /// <summary>Enqueues an asset's uploaded dark variant for processing.</summary>
    public void EnqueueDarkVariant(AssetId assetId) =>
        _queue.Writer.TryWrite(new AssetProcessingItem(assetId, AssetProcessingKind.DarkVariant));

    public ChannelReader<AssetProcessingItem> Reader => _queue.Reader;
}
