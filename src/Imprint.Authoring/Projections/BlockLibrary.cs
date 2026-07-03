using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Blocks;
using Imprint.Authoring.Domain.Blocks.Events;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Projections;

/// <summary>
/// The symbols library, folded through the BlockDefinition aggregate
/// (same pattern as <see cref="PageDrafts"/>).
/// </summary>
public sealed class BlockLibrary : ReadModel
{
    private readonly Dictionary<BlockDefinitionId, BlockDefinition> _blocks = [];

    public BlockDefinition? Get(BlockDefinitionId id) => _blocks.GetValueOrDefault(id);

    public IReadOnlyList<BlockDefinition> All() =>
        [.. _blocks.Values.OrderBy(block => block.Name, StringComparer.OrdinalIgnoreCase)];

    public override void Apply(StoredEvent @event)
    {
        if (StreamIds.IdOf(@event.StreamId, "block-") is not { } guid)
        {
            return;
        }

        var id = BlockDefinitionId.From(guid);
        if (@event.Event is BlockDefined)
        {
            var block = new BlockDefinition();
            block.LoadFrom([@event.Event]);
            _blocks[id] = block;
        }
        else if (_blocks.TryGetValue(id, out var block))
        {
            block.LoadFrom([@event.Event]);
            if (block.IsDeleted)
            {
                _blocks.Remove(id);
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Block event {@event.StableId} for unknown block {id} — corrupt sequence?");
        }

        NotifyChanged();
    }

    public override void Reset() => _blocks.Clear();
}
