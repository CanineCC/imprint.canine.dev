using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Blocks.UpdateBlockFromInstance;

/// <summary>
/// "Push to block": takes what one placed instance currently shows (definition +
/// its overrides) and makes that the definition itself, updating every instance.
/// </summary>
public sealed record UpdateBlockFromInstance(PageId PageId, NodeId InstanceId) : ICommand;
