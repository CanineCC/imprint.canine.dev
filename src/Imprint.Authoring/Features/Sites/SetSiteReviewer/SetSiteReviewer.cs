using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.SetSiteReviewer;

/// <summary>Names (or clears, with a blank email) the site's public-relations reviewer.</summary>
public sealed record SetSiteReviewer(SiteId SiteId, string? Name, string? Email) : ICommand;
