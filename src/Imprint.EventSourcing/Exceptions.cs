namespace Imprint.EventSourcing;

/// <summary>
/// An aggregate invariant was violated. Aggregates throw this from behavior methods;
/// the <see cref="CommandDispatcher"/> converts it into a failed <see cref="Result"/>
/// whose message is shown to the user — so write these messages for humans.
/// </summary>
public class DomainException(string message) : Exception(message);

/// <summary>
/// Optimistic concurrency conflict: the stream advanced past the expected version
/// between load and append. The dispatcher retries the whole command (reload,
/// re-decide) a bounded number of times before giving up.
/// </summary>
public sealed class ConcurrencyException(string streamId, long expectedVersion)
    : Exception($"Stream '{streamId}' was modified concurrently (expected version {expectedVersion}).")
{
    public string StreamId { get; } = streamId;
    public long ExpectedVersion { get; } = expectedVersion;
}

/// <summary>Thrown by <see cref="IAggregateStore.Load{T}"/> when the stream has no events.</summary>
public sealed class StreamNotFoundException(string streamId)
    : Exception($"Stream '{streamId}' does not exist.")
{
    public string StreamId { get; } = streamId;
}
