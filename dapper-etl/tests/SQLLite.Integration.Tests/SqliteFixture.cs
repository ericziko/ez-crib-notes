using System.Data;
using Microsoft.Data.Sqlite;

namespace SQLLite.Integration.Tests;

/// <summary>
///     Fixture that provides a SQLite database connection for integration tests.
/// </summary>
public class SqliteFixture : IAsyncLifetime {
    private readonly string _databasePath;
    private SqliteConnection? _connection;

    public SqliteFixture() {
        _databasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
    }

    public IDbConnection Connection {
        get {
            if (_connection == null) {
                throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");
            }
            return _connection;
        }
    }

    public async Task InitializeAsync() {
        var connectionString = $"Data Source={_databasePath};";
        _connection = new SqliteConnection(connectionString);
        await Task.Run(() => _connection.Open());
        await CreateTablesAsync();
    }

    public async Task DisposeAsync() {
        if (_connection != null) {
            await Task.Run(() => {
                _connection.Close();
                _connection.Dispose();
            });
        }

        if (File.Exists(_databasePath)) {
            File.Delete(_databasePath);
        }
    }

    private async Task CreateTablesAsync() {
        if (_connection == null) {
            throw new InvalidOperationException("Connection not initialized.");
        }

        // Create source table
        var createSourceSql = @"
                CREATE TABLE SourceTable (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Value INTEGER NOT NULL,
                    Description TEXT
                )";

        // Create destination table
        var createDestSql = @"
                CREATE TABLE DestTable (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Value INTEGER NOT NULL,
                    Description TEXT
                )";

        await Task.Run(() => {
            using (var cmd = _connection.CreateCommand()) {
                cmd.CommandText = createSourceSql;
                cmd.ExecuteNonQuery();
            }

            using (var cmd = _connection.CreateCommand()) {
                cmd.CommandText = createDestSql;
                cmd.ExecuteNonQuery();
            }
        });
    }

    public async Task InsertSourceDataAsync(int id, string name, int value, string? description = null) {
        if (_connection == null) {
            throw new InvalidOperationException("Connection not initialized.");
        }

        await Task.Run(() => {
            using (var cmd = _connection.CreateCommand()) {
                cmd.CommandText = @"
                        INSERT INTO SourceTable (Id, Name, Value, Description)
                        VALUES (@id, @name, @value, @description)";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@value", value);
                cmd.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        });
    }

    public async Task<int> GetDestTableCountAsync() {
        if (_connection == null) {
            throw new InvalidOperationException("Connection not initialized.");
        }

        return await Task.Run(() => {
            using (var cmd = _connection.CreateCommand()) {
                cmd.CommandText = "SELECT COUNT(*) FROM DestTable";
                var result = cmd.ExecuteScalar();
                return result != null ? (int)(long)result : 0;
            }
        });
    }

    public async Task TruncateSourceTableAsync() {
        if (_connection == null) {
            throw new InvalidOperationException("Connection not initialized.");
        }

        await Task.Run(() => {
            using (var cmd = _connection.CreateCommand()) {
                cmd.CommandText = "DELETE FROM SourceTable";
                cmd.ExecuteNonQuery();
            }
        });
    }

    public async Task TruncateDestTableAsync() {
        if (_connection == null) {
            throw new InvalidOperationException("Connection not initialized.");
        }

        await Task.Run(() => {
            using (var cmd = _connection.CreateCommand()) {
                cmd.CommandText = "DELETE FROM DestTable";
                cmd.ExecuteNonQuery();
            }
        });
    }
}