namespace Imprint.EventSourcing;

/// <summary>
/// Envelope metadata stored beside every event: who caused it, when, and the
/// correlation/causation pair that lets you trace a user action through every event
/// it produced.
/// </summary>
public sealed record EventMetadata(
    string Actor,
    DateTimeOffset TimestampUtc,
    Guid CorrelationId,
    Guid CausationId);

/// <summary>
/// Supplies metadata for the events of the command currently being dispatched.
/// The dispatcher opens a scope per command; anything appended inside it (including
/// from nested handler logic) shares one correlation id. Flows across awaits via
/// <see cref="AsyncLocal{T}"/>.
/// </summary>
public sealed class EventMetadataProvider
{
    private static readonly AsyncLocal<EventMetadata?> Current = new();

    /// <summary>
    /// The identity recorded as <see cref="EventMetadata.Actor"/>. Single-tenant v1
    /// default is the OS user; an auth integration replaces this delegate at startup.
    /// </summary>
    public Func<string> ActorSource { get; set; } = () => Environment.UserName;

    public EventMetadata GetCurrent() =>
        Current.Value ?? Create(ActorSource(), Guid.NewGuid());

    public IDisposable BeginCommandScope()
    {
        Current.Value = Create(ActorSource(), Guid.NewGuid());
        return new Scope();
    }

    private static EventMetadata Create(string actor, Guid correlationId) =>
        new(actor, DateTimeOffset.UtcNow, correlationId, correlationId);

    private sealed class Scope : IDisposable
    {
        public void Dispose() => Current.Value = null;
    }
}
