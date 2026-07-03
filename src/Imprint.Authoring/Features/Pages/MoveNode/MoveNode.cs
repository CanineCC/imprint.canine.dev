using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.MoveNode;

public sealed record MoveNode(PageId PageId, NodeId NodeId, NodeId NewParentId, int NewIndex) : ICommand;
