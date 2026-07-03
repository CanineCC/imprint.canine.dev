using Imprint.Authoring.Domain;
using Imprint.Authoring.Domain.Sites;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ChangeNavigation;

// The full list travels in one command: navigation is small (≤ 20 items) and the
// editor reorders it as a unit, mirroring the site.navigation-changed event.
public sealed record ChangeNavigation(SiteId SiteId, IReadOnlyList<NavigationItem> Items) : ICommand;
