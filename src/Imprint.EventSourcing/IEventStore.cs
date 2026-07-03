namespace Imprint.EventSourcing;

/// <summary>
/// An event as read back from the store: the deserialized domain event plus its
/// position in the stream and in the global sequence.
/// </summary>
public sealed record StoredEvent(
    long GlobalPosition,
    string StreamId,
    long StreamVersion,
    string StableId,
    object Event,
    EventMetadata Metadata);

/// <summary>
/// The append-only source of truth. Streams are named (<c>page-{guid}</c>), versions
/// are 1-based and contiguous per stream, and the global position totally orders all
/// events — which is what makes projections deterministic.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events atomically. <paramref name="expectedVersion"/> is the version
    /// the caller last observed (0 = the stream must not exist yet); a mismatch
    /// throws <see cref="ConcurrencyException"/>. Returns the new stream version.
    /// </summary>
    Task<long> Append(
        string streamId,
        long expectedVersion,
        IReadOnlyList<object> events,
        EventMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Reads a stream in order, optionally only up to a version — which is exactly how
    /// the delivery plane reads "the page as it was when published".
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> ReadStream(
        string streamId,
        long toVersionInclusive = long.MaxValue,
        CancellationToken ct = default);

    /// <summary>Reads the global sequence after a position — the projection feed.</summary>
    Task<IReadOnlyList<StoredEvent>> ReadAll(
        long afterPosition,
        int maxCount,
        CancellationToken ct = default);

    /// <summary>Highest global position, or 0 when empty.</summary>
    Task<long> GetLastPosition(CancellationToken ct = default);
}
