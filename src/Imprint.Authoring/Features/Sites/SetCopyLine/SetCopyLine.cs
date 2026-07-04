using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetCopyLine;

// The footer's fine-print copy line (null clears it). Non-empty is an aggregate invariant.
public sealed record SetCopyLine(SiteId SiteId, CopyLine? CopyLine) : ICommand;
