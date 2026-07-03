using System.Threading.Channels;
using Imprint.Authoring.Domain;

namespace Imprint.Authoring.Features.Assets.ProcessUploadedAsset;

/// <summary>
/// The in-process hand-off between upload and derivative processing: an unbounded
/// <see cref="Channel{T}"/> of asset ids. Unbounded is deliberate — ids are tiny, and
/// an upload must never block behind a slow transcode. The queue carries no state
/// worth persisting: whatever is lost in a crash is re-derived at startup from asset
/// status (see <see cref="AssetProcessingWorker"/>).
/// </summary>
public sealed class AssetProcessingQueue
{
    private readonly Channel<AssetId> _queue = Channel.CreateUnbounded<AssetId>();

    public void Enqueue(AssetId assetId) => _queue.Writer.TryWrite(assetId);

    public ChannelReader<AssetId> Reader => _queue.Reader;
}
