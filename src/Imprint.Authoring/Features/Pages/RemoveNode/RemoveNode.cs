using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.RemoveNode;

public sealed record RemoveNode(PageId PageId, NodeId NodeId) : ICommand;
