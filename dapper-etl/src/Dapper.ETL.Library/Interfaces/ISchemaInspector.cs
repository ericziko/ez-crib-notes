using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dapper.ETL.Library.Interfaces;

/// <summary>
/// Provides database-agnostic schema introspection for retrieving column names from tables.
/// </summary>
public interface ISchemaInspector {
    /// <summary>
    /// Gets the column names for the specified table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>A list of column names ordered by their position in the table.</returns>
    Task<List<string>> GetColumnNamesAsync(string tableName);
}