using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DuplicateNode;

public sealed record DuplicateNode(PageId PageId, NodeId NodeId) : ICommand;
