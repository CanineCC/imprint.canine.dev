using Microsoft.Data.Sqlite;

namespace Imprint.EventSourcing;

/// <summary>
/// The hand-rolled event store: one SQLite table, an autoincrement global position,
/// and a unique (stream, version) index that *is* the optimistic concurrency control.
/// Deliberately boring — the point of Imprint is that this is all an event store needs
/// to be for a single-node system.
/// </summary>
public sealed class SqliteEventStore(string connectionString, EventRegistry registry) : IEventStore
{
    public async Task EnsureSchema(CancellationToken ct = default)
    {
        await using var connection = await Open(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS events (
                global_position INTEGER PRIMARY KEY AUTOINCREMENT,
                stream_id       TEXT    NOT NULL,
                stream_version  INTEGER NOT NULL,
                stable_id       TEXT    NOT NULL,
                data            TEXT    NOT NULL,
                metadata        TEXT    NOT NULL,
                UNIQUE (stream_id, stream_version)
            );
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<long> Append(
        string streamId,
        long expectedVersion,
        IReadOnlyList<object> events,
        EventMetadata metadata,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamId);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);
        if (events.Count == 0)
        {
            return expectedVersion;
        }

        await using var connection = await Open(ct);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

        // Read-then-insert inside one transaction gives a precise error; the UNIQUE
        // index catches the write-write race the read cannot see (two appenders past
        // the read at the same time).
        await using (var check = connection.CreateCommand())
        {
            check.Transaction = transaction;
            check.CommandText = "SELECT COALESCE(MAX(stream_version), 0) FROM events WHERE stream_id = $stream";
            check.Parameters.AddWithValue("$stream", streamId);
            var actual = (long)(await check.ExecuteScalarAsync(ct))!;
            if (actual != expectedVersion)
            {
                throw new ConcurrencyException(streamId, expectedVersion);
            }
        }

        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata, registry.JsonOptions);
        var version = expectedVersion;
        foreach (var @event in events)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO events (stream_id, stream_version, stable_id, data, metadata)
                VALUES ($stream, $version, $stableId, $data, $metadata)
                """;
            insert.Parameters.AddWithValue("$stream", streamId);
            insert.Parameters.AddWithValue("$version", ++version);
            insert.Parameters.AddWithValue("$stableId", registry.StableIdOf(@event));
            insert.Parameters.AddWithValue("$data", registry.Serialize(@event));
            insert.Parameters.AddWithValue("$metadata", metadataJson);
            try
            {
                await insert.ExecuteNonQueryAsync(ct);
            }
            catch (SqliteException e) when (e.SqliteErrorCode == 19 /* constraint */)
            {
                throw new ConcurrencyException(streamId, expectedVersion);
            }
        }

        await transaction.CommitAsync(ct);
        return version;
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadStream(
        string streamId,
        long toVersionInclusive = long.MaxValue,
        CancellationToken ct = default)
    {
        await using var connection = await Open(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT global_position, stream_id, stream_version, stable_id, data, metadata
            FROM events
            WHERE stream_id = $stream AND stream_version <= $toVersion
            ORDER BY stream_version
            """;
        command.Parameters.AddWithValue("$stream", streamId);
        command.Parameters.AddWithValue("$toVersion", toVersionInclusive);
        return await Materialize(command, ct);
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadAll(
        long afterPosition,
        int maxCount,
        CancellationToken ct = default)
    {
        await using var connection = await Open(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT global_position, stream_id, stream_version, stable_id, data, metadata
            FROM events
            WHERE global_position > $after
            ORDER BY global_position
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$after", afterPosition);
        command.Parameters.AddWithValue("$limit", maxCount);
        return await Materialize(command, ct);
    }

    public async Task<long> GetLastPosition(CancellationToken ct = default)
    {
        await using var connection = await Open(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(global_position), 0) FROM events";
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    private async Task<SqliteConnection> Open(CancellationToken ct)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);

        // WAL lets the editor read projections while a command is appending.
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;";
        await pragma.ExecuteNonQueryAsync(ct);
        return connection;
    }

    private async Task<IReadOnlyList<StoredEvent>> Materialize(SqliteCommand command, CancellationToken ct)
    {
        var results = new List<StoredEvent>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var stableId = reader.GetString(3);
            results.Add(new StoredEvent(
                GlobalPosition: reader.GetInt64(0),
                StreamId: reader.GetString(1),
                StreamVersion: reader.GetInt64(2),
                StableId: stableId,
                Event: registry.Deserialize(stableId, reader.GetString(4)),
                Metadata: System.Text.Json.JsonSerializer.Deserialize<EventMetadata>(
                    reader.GetString(5), registry.JsonOptions)!));
        }

        return results;
    }
}
