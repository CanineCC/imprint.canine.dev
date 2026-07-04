using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetHeaderActions;

// The header's primary CTA and quiet link share a slot and are set together (either may
// be null to clear it). Label-present is an aggregate invariant.
public sealed record SetHeaderActions(SiteId SiteId, HeaderAction? Cta, HeaderAction? Quiet) : ICommand;
