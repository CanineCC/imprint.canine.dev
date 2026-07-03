using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DetachBlockInstance;

public sealed record DetachBlockInstance(PageId PageId, NodeId InstanceId) : ICommand;
