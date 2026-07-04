using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetFooter;

// The whole footer travels in one command: like navigation, the editor edits the columns
// as a unit and mirrors the site.footer-changed event. Shape (heading present, per-group
// link cap, external-link labels) is an aggregate invariant with human-readable messages.
public sealed record SetFooter(SiteId SiteId, IReadOnlyList<FooterLinkGroup> Groups) : ICommand;
