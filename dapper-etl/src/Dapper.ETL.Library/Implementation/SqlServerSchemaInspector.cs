using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper.ETL.Library.Interfaces;

namespace Dapper.ETL.Library.Implementation;

/// <summary>
/// SQL Server schema inspector using INFORMATION_SCHEMA.COLUMNS.
/// </summary>
public class SqlServerSchemaInspector : ISchemaInspector {
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSchemaInspector" /> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">Optional transaction.</param>
    public SqlServerSchemaInspector(IDbConnection connection, IDbTransaction? transaction = null) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    /// <summary>
    /// Gets column names from SQL Server using INFORMATION_SCHEMA.COLUMNS.
    /// </summary>
    public async Task<List<string>> GetColumnNamesAsync(string tableName) {
        if (string.IsNullOrWhiteSpace(tableName)) {
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
        }

        var query = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

        var columns = (await _connection.QueryAsync<string>(
            query,
            new { TableName = tableName },
            _transaction)).ToList();

        return columns;
    }
}