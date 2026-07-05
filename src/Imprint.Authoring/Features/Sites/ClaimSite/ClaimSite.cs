using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ClaimSite;

// The claimant is the command's envelope actor — the signed-in user — so the command
// carries only the site. The UI offers a claim only while SiteOverview.IsUnclaimed.
public sealed record ClaimSite(SiteId SiteId) : ICommand;
