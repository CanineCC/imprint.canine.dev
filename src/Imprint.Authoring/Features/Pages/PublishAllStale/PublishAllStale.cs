using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.PublishAllStale;

/// <summary>Publishes every stale page <em>of one site</em> — a draft ahead of (or never given) a publish.</summary>
public sealed record PublishAllStale(SiteId SiteId) : ICommand;
