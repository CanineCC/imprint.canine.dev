using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetRobotsDisallowedPaths;

// Which robots.txt paths this site asks crawlers to skip ("Disallow:" lines). Null or empty clears the policy.
// Pages stay published and in the sitemap: this is a crawl policy for paths that share the host, not an unpublish.
public sealed record SetRobotsDisallowedPaths(SiteId SiteId, IReadOnlyList<string>? Paths) : ICommand;
