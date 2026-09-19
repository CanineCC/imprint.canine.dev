using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetByline;

// The footer's "by <name>" attribution. Null keeps the publisher default; a Byline with an empty
// name renders no attribution at all.
public sealed record SetByline(SiteId SiteId, Byline? Byline) : ICommand;
