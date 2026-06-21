using Microsoft.Data.Sqlite;

namespace NSchema.SQLite;

/// <summary>
/// Holds the SQLite connection string and opens connections for the schema provider. SQLite has no pooled
/// "data source" abstraction (as Npgsql does), so this lightweight source plays the equivalent role: it is the
/// single DI-registered object the provider depends on, and the seam through which a connection string is supplied.
/// </summary>
internal sealed class SqliteConnectionSource(string connectionString)
{
    /// <summary>The connection string used to open connections.</summary>
    public string ConnectionString { get; } = connectionString;

    /// <summary>Opens a new connection to the configured SQLite database.</summary>
    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
