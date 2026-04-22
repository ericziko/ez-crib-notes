using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper.ETL.Library.Interfaces;

namespace Dapper.ETL.Library.Implementation;

/// <summary>
///     SQLite schema inspector using PRAGMA table_info.
/// </summary>
public class SqliteSchemaInspector : ISchemaInspector {
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteSchemaInspector" /> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">Optional transaction.</param>
    public SqliteSchemaInspector(IDbConnection connection, IDbTransaction? transaction = null) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    /// <summary>
    ///     Gets column names from SQLite using PRAGMA table_info.
    /// </summary>
    public async Task<List<string>> GetColumnNamesAsync(string tableName) {
        if (string.IsNullOrWhiteSpace(tableName)) {
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        }

        var query = $"SELECT name FROM pragma_table_info('{tableName}')";

        var columns = (await _connection.QueryAsync<string>(
            query,
            transaction: _transaction)).ToList();

        return columns;
    }
}