namespace Imprint.EventSourcing;

/// <summary>
/// Base class for event-sourced aggregates using the classic Raise/When split:
/// behavior methods validate invariants and <see cref="Raise"/> events; <see cref="When"/>
/// is the only place state mutates, and it must never fail — by the time an event
/// exists it is a fact.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<object> _uncommitted = [];

    /// <summary>The stream this aggregate persists to, e.g. <c>page-3f2a…</c>. Derived from the aggregate id.</summary>
    public abstract string StreamId { get; }

    /// <summary>The last *committed* stream version; uncommitted events are not counted.</summary>
    public long Version { get; private set; }

    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    protected void Raise(object @event)
    {
        When(@event);
        _uncommitted.Add(@event);
    }

    /// <summary>Folds one event into state. Exhaustive switch; unknown events must throw — silence hides bugs.</summary>
    protected abstract void When(object @event);

    public void LoadFrom(IEnumerable<object> history)
    {
        foreach (var @event in history)
        {
            When(@event);
            Version++;
        }
    }

    public void MarkCommitted(long newVersion)
    {
        _uncommitted.Clear();
        Version = newVersion;
    }
}

/// <summary>Loads and saves aggregates against the event store.</summary>
public interface IAggregateStore
{
    /// <summary>Loads an aggregate or throws <see cref="StreamNotFoundException"/>.</summary>
    Task<T> Load<T>(string streamId, CancellationToken ct = default) where T : AggregateRoot, new();

    /// <summary>Loads an aggregate or returns null when the stream doesn't exist.</summary>
    Task<T?> LoadOrDefault<T>(string streamId, CancellationToken ct = default) where T : AggregateRoot, new();

    /// <summary>
    /// Appends the aggregate's uncommitted events at its loaded version. Concurrency
    /// conflicts throw <see cref="ConcurrencyException"/> — callers don't retry here,
    /// the command dispatcher re-runs the whole decision against fresh state.
    /// </summary>
    Task Save(AggregateRoot aggregate, CancellationToken ct = default);
}

public sealed class AggregateStore(IEventStore store, EventMetadataProvider metadata) : IAggregateStore
{
    public async Task<T> Load<T>(string streamId, CancellationToken ct = default)
        where T : AggregateRoot, new()
    {
        var events = await store.ReadStream(streamId, ct: ct);
        if (events.Count == 0)
        {
            throw new StreamNotFoundException(streamId);
        }

        var aggregate = new T();
        aggregate.LoadFrom(events.Select(e => e.Event));
        return aggregate;
    }

    public async Task<T?> LoadOrDefault<T>(string streamId, CancellationToken ct = default)
        where T : AggregateRoot, new()
    {
        var events = await store.ReadStream(streamId, ct: ct);
        if (events.Count == 0)
        {
            return null;
        }

        var aggregate = new T();
        aggregate.LoadFrom(events.Select(e => e.Event));
        return aggregate;
    }

    public async Task Save(AggregateRoot aggregate, CancellationToken ct = default)
    {
        if (aggregate.UncommittedEvents.Count == 0)
        {
            return;
        }

        var newVersion = await store.Append(
            aggregate.StreamId,
            expectedVersion: aggregate.Version,
            aggregate.UncommittedEvents,
            metadata.GetCurrent(),
            ct);
        aggregate.MarkCommitted(newVersion);
    }
}
