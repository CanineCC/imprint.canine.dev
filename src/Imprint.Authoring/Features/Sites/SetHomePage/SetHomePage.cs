using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetHomePage;

// Which page is served at the site root. A null page id clears the choice and restores the
// legacy rule (the nav-first page). Whether the page exists and belongs to this site is
// checked by the handler — the aggregate only records the choice.
public sealed record SetHomePage(SiteId SiteId, PageId? PageId) : ICommand;
