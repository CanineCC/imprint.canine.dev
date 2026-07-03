using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.DefineBlockFromNode;

public sealed record DefineBlockFromNode(
    PageId PageId,
    NodeId NodeId,
    BlockDefinitionId NewBlockId,
    string Name) : ICommand;
