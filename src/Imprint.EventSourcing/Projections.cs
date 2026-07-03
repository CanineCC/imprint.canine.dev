using Microsoft.Extensions.Logging;

namespace Imprint.EventSourcing;

/// <summary>
/// A read model: a pure, in-memory fold over the global event sequence. Projections
/// are rebuilt by full replay at startup — derived state is disposable by design.
/// A projection that throws is a bug and must surface; the engine never skips events.
/// </summary>
public interface IProjection
{
    void Apply(StoredEvent @event);

    /// <summary>Return to the empty state (start of the sequence). Used for rebuilds.</summary>
    void Reset();
}

/// <summary>
/// Pull-based catch-up: the dispatcher pokes <see cref="CatchUp"/> after every
/// successful command; the engine reads the global sequence from its position under a
/// gate, so projections always see events in order exactly once — regardless of how
/// many Blazor circuits are appending concurrently.
/// </summary>
public sealed class ProjectionEngine(
    IEventStore store,
    IEnumerable<IProjection> projections,
    ILogger<ProjectionEngine> logger)
{
    private const int BatchSize = 512;

    private readonly IReadOnlyList<IProjection> _projections = [.. projections];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _position;

    public long Position => Interlocked.Read(ref _position);

    /// <summary>Raised (outside the gate) after new events were folded; carries the new position.</summary>
    public event Action<long>? CaughtUp;

    public async Task CatchUp(CancellationToken ct = default)
    {
        long? reached = null;
        await _gate.WaitAsync(ct);
        try
        {
            while (true)
            {
                var batch = await store.ReadAll(_position, BatchSize, ct);
                if (batch.Count == 0)
                {
                    break;
                }

                foreach (var @event in batch)
                {
                    foreach (var projection in _projections)
                    {
                        projection.Apply(@event);
                    }

                    _position = @event.GlobalPosition;
                }

                reached = _position;
            }
        }
        finally
        {
            _gate.Release();
        }

        if (reached is { } position)
        {
            CaughtUp?.Invoke(position);
        }
    }

    /// <summary>Full rebuild: reset every projection and replay the world. Called at startup.</summary>
    public async Task Rebuild(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            foreach (var projection in _projections)
            {
                projection.Reset();
            }

            _position = 0;
        }
        finally
        {
            _gate.Release();
        }

        var started = TimeProvider.System.GetTimestamp();
        await CatchUp(ct);
        logger.LogInformation(
            "Projections rebuilt to position {Position} in {Elapsed:F0} ms.",
            Position, TimeProvider.System.GetElapsedTime(started).TotalMilliseconds);
    }
}
