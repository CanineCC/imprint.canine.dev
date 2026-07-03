using Imprint.EventSourcing;
using Imprint.TestKit;

namespace Imprint.EventSourcing.Tests;

[EventType("test.something-happened")]
public sealed record SomethingHappened(string What, int HowMuch);

[EventType("test.something-else-happened")]
public sealed record SomethingElseHappened(Guid Which);

public sealed class SqliteEventStoreTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly SqliteEventStore _store;
    private static readonly EventMetadata Metadata = new("tests", DateTimeOffset.UnixEpoch, Guid.NewGuid(), Guid.NewGuid());

    public SqliteEventStoreTests()
    {
        var registry = new EventRegistry([typeof(SqliteEventStoreTests).Assembly]);
        _store = new SqliteEventStore(_database.ConnectionString, registry);
        _store.EnsureSchema().GetAwaiter().GetResult();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Append_then_read_round_trips_events_in_order()
    {
        var events = new object[] { new SomethingHappened("a", 1), new SomethingHappened("b", 2) };
        var version = await _store.Append("stream-1", 0, events, Metadata);

        Assert.Equal(2, version);
        var read = await _store.ReadStream("stream-1");
        Assert.Equal(events, read.Select(e => e.Event));
        Assert.Equal([1L, 2L], read.Select(e => e.StreamVersion));
    }

    [Fact]
    public async Task Append_with_stale_expected_version_throws_concurrency()
    {
        await _store.Append("stream-1", 0, [new SomethingHappened("a", 1)], Metadata);

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            _store.Append("stream-1", 0, [new SomethingHappened("b", 2)], Metadata));
    }

    [Fact]
    public async Task Append_to_new_stream_requires_expected_version_zero()
    {
        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            _store.Append("stream-1", 5, [new SomethingHappened("a", 1)], Metadata));
    }

    [Fact]
    public async Task ReadStream_honors_version_ceiling()
    {
        await _store.Append("stream-1", 0,
            [new SomethingHappened("a", 1), new SomethingHappened("b", 2), new SomethingHappened("c", 3)], Metadata);

        var read = await _store.ReadStream("stream-1", toVersionInclusive: 2);
        Assert.Equal(2, read.Count);
        Assert.Equal(new SomethingHappened("b", 2), read[^1].Event);
    }

    [Fact]
    public async Task ReadAll_totally_orders_across_streams()
    {
        await _store.Append("stream-a", 0, [new SomethingHappened("a", 1)], Metadata);
        await _store.Append("stream-b", 0, [new SomethingElseHappened(Guid.Empty)], Metadata);
        await _store.Append("stream-a", 1, [new SomethingHappened("a2", 2)], Metadata);

        var all = await _store.ReadAll(afterPosition: 0, maxCount: 100);
        Assert.Equal(3, all.Count);
        Assert.Equal([1L, 2L, 3L], all.Select(e => e.GlobalPosition));
        Assert.Equal(["stream-a", "stream-b", "stream-a"], all.Select(e => e.StreamId));

        var afterFirst = await _store.ReadAll(afterPosition: 1, maxCount: 100);
        Assert.Equal(2, afterFirst.Count);
    }

    [Fact]
    public async Task Metadata_round_trips()
    {
        await _store.Append("stream-1", 0, [new SomethingHappened("a", 1)], Metadata);
        var read = await _store.ReadStream("stream-1");
        Assert.Equal(Metadata, read[0].Metadata);
    }

    [Fact]
    public async Task Concurrent_appends_to_one_stream_admit_exactly_one_writer()
    {
        await _store.Append("stream-1", 0, [new SomethingHappened("seed", 0)], Metadata);

        var attempts = Enumerable.Range(0, 8).Select(i =>
            _store.Append("stream-1", 1, [new SomethingHappened($"racer-{i}", i)], Metadata));
        var outcomes = await Task.WhenAll(attempts.Select(async task =>
        {
            try
            {
                await task;
                return true;
            }
            catch (ConcurrencyException)
            {
                return false;
            }
        }));

        Assert.Equal(1, outcomes.Count(won => won));
        Assert.Equal(2, (await _store.ReadStream("stream-1")).Count);
    }
}
