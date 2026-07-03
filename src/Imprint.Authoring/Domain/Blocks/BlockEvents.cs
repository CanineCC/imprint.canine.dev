using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Domain.Blocks.Events;

// The BlockDefinition aggregate's events (docs/domain-model.md §4). The spec is a
// Node subtree; node value equality is structural, so these records compare by value.

[EventType("block.defined")]
public sealed record BlockDefined(BlockDefinitionId BlockDefinitionId, string Name, Node Spec);

[EventType("block.renamed")]
public sealed record BlockRenamed(string Name);

[EventType("block.spec-changed")]
public sealed record BlockSpecChanged(Node Spec);

[EventType("block.deleted")]
public sealed record BlockDeleted;
