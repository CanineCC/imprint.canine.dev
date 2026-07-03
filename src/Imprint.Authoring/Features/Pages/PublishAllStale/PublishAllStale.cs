using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.PublishAllStale;

/// <summary>Publishes every page whose draft is ahead of (or has never had) a publish.</summary>
public sealed record PublishAllStale : ICommand;
