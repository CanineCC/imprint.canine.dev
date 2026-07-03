using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Pages;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.AddNode;

public sealed record AddNode(PageId PageId, NodeId ParentId, int Index, Node Spec) : ICommand;
