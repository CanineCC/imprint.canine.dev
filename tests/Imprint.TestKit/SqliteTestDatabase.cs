using Microsoft.Data.Sqlite;

namespace Imprint.TestKit;

/// <summary>
/// A private shared-cache in-memory SQLite database that lives exactly as long as this
/// fixture: the keep-alive connection below pins it open while pooled connections from
/// the store come and go.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _keepAlive;

    public SqliteTestDatabase()
    {
        var name = $"imprint-test-{Guid.NewGuid():N}";
        ConnectionString = $"Data Source={name};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();
    }

    public string ConnectionString { get; }

    public void Dispose()
    {
        _keepAlive.Dispose();
        SqliteConnection.ClearAllPools();
    }
}
