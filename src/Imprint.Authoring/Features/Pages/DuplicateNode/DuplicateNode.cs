using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.DuplicateNode;

/// <summary>
/// Duplicates a node. <paramref name="CopyId"/> is the id the copy's root will take —
/// supplied by the caller so it can register a <c>RemoveNode(CopyId)</c> inverse for
/// undo. Descendant ids are minted in the aggregate and recorded in the event.
/// </summary>
public sealed record DuplicateNode(PageId PageId, NodeId NodeId, NodeId CopyId) : ICommand;
