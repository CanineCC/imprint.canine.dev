using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Assets;
using Imprint.Authoring.Domain.Assets.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// Asset panel + render-resolution read model, folded through the Asset aggregate
/// (same pattern as <see cref="PageDrafts"/>). Ordering for the panel: newest first.
/// </summary>
public sealed class AssetLibrary : ReadModel
{
    private readonly Dictionary<AssetId, Asset> _assets = [];
    private readonly Dictionary<AssetId, DateTimeOffset> _updated = [];

    public Asset? Get(AssetId id) => _assets.GetValueOrDefault(id);

    public IReadOnlyList<Asset> All() =>
        [.. _assets.Values.OrderByDescending(asset => _updated[asset.Id])];

    public DateTimeOffset UpdatedAt(AssetId id) => _updated.GetValueOrDefault(id);

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "asset-") is not { } guid)
        {
            return;
        }

        var id = AssetId.From(guid);
        if (@event.Event is AssetUploaded)
        {
            var asset = new Asset();
            asset.LoadFrom([@event.Event]);
            _assets[id] = asset;
        }
        else if (_assets.TryGetValue(id, out var asset))
        {
            asset.LoadFrom([@event.Event]);
            if (asset.IsDeleted)
            {
                _assets.Remove(id);
                _updated.Remove(id);
                NotifyChanged();
                return;
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Asset event {@event.StableId} for unknown asset {id} — corrupt sequence?");
        }

        _updated[id] = @event.Metadata.TimestampUtc;
        NotifyChanged();
    }

    public override void Reset()
    {
        _assets.Clear();
        _updated.Clear();
    }
}
