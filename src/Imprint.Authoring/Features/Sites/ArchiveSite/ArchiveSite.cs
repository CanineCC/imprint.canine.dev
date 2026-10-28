using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Sites.ArchiveSite;

public sealed record ArchiveSite(SiteId SiteId) : ICommand;

public sealed record RestoreSite(SiteId SiteId) : ICommand;
