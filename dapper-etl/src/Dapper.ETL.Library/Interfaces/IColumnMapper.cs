using System.Collections.Generic;
using Dapper.ETL.Library.Models;

namespace Dapper.ETL.Library.Interfaces;

/// <summary>
///     Interface for mapping columns between source and destination tables.
/// </summary>
public interface IColumnMapper {
    /// <summary>
    ///     Gets the column mappings for a table copy operation.
    /// </summary>
    /// <param name="sourceColumns">The columns in the source table.</param>
    /// <param name="destinationColumns">The columns in the destination table.</param>
    /// <param name="mappingOverrides">Optional overrides for column mappings.</param>
    /// <returns>A collection of column mappings.</returns>
    IEnumerable<ColumnMapping> GetMapping(
        IEnumerable<string> sourceColumns,
        IEnumerable<string> destinationColumns,
        IDictionary<string, string>? mappingOverrides = null);

    /// <summary>
    ///     Generates a SELECT clause from the given column mappings.
    /// </summary>
    /// <param name="mappings">The column mappings.</param>
    /// <returns>The SELECT clause as a string.</returns>
    string GetSelectClause(IEnumerable<ColumnMapping> mappings);

    /// <summary>
    ///     Generates an INSERT clause from the given column mappings.
    /// </summary>
    /// <param name="mappings">The column mappings.</param>
    /// <returns>The INSERT clause as a string.</returns>
    string GetInsertClause(IEnumerable<ColumnMapping> mappings);
}