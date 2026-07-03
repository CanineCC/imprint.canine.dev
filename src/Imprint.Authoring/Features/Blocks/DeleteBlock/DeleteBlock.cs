using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.DeleteBlock;

public sealed record DeleteBlock(BlockDefinitionId BlockId) : ICommand;
