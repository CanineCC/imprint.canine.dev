using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.RenameBlock;

// Name shape (empty, length) is validated by the aggregate with human messages.
public sealed record RenameBlock(BlockDefinitionId BlockId, string Name) : ICommand;
