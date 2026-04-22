using System;
using System.Collections.Generic;
using System.Linq;
using Dapper.ETL.Library.Interfaces;
using Dapper.ETL.Library.Models;

namespace Dapper.ETL.Library.Implementation;

/// <summary>
///     Default implementation of column mapping.
/// </summary>
public class ColumnMapper : IColumnMapper {
    /// <summary>
    ///     Gets the column mappings for a table copy operation.
    /// </summary>
    /// <param name="sourceColumns">The columns in the source table.</param>
    /// <param name="destinationColumns">The columns in the destination table.</param>
    /// <param name="mappingOverrides">Optional overrides for column mappings.</param>
    /// <returns>A collection of column mappings.</returns>
    public IEnumerable<ColumnMapping> GetMapping(
        IEnumerable<string> sourceColumns,
        IEnumerable<string> destinationColumns,
        IDictionary<string, string>? mappingOverrides = null) {
        var sourceList = sourceColumns?.ToList() ?? throw new ArgumentNullException(nameof(sourceColumns));
        var destinationList = destinationColumns?.ToList() ?? throw new ArgumentNullException(nameof(destinationColumns));
        var overrides = mappingOverrides ?? new Dictionary<string, string>();

        var mappings = new List<ColumnMapping>();

        foreach (var sourceCol in sourceList) {
            // Check for override first
            if (overrides.TryGetValue(sourceCol, out var overrideDest)) {
                mappings.Add(new ColumnMapping(sourceCol, overrideDest));
            }
            // Check for exact match
            else if (destinationList.Contains(sourceCol, StringComparer.OrdinalIgnoreCase)) {
                // Find the actual destination column name (preserving case)
                var destCol = destinationList.First(d => d.Equals(sourceCol, StringComparison.OrdinalIgnoreCase));
                mappings.Add(new ColumnMapping(sourceCol, destCol));
            }
        }

        return mappings;
    }

    /// <summary>
    ///     Generates a SELECT clause from the given column mappings.
    /// </summary>
    /// <param name="mappings">The column mappings.</param>
    /// <returns>The SELECT clause as a string.</returns>
    public string GetSelectClause(IEnumerable<ColumnMapping> mappings) {
        var mappingsList = mappings?.ToList() ?? throw new ArgumentNullException(nameof(mappings));

        if (mappingsList.Count == 0) {
            return string.Empty;
        }

        var columnNames = mappingsList.Select(m => {
            if (string.IsNullOrWhiteSpace(m.SelectExpression)) {
                return $"[{m.SourceColumn}]";
            }
            return $"{m.SelectExpression} AS [{m.DestinationColumn}]";
        });
        return string.Join(", ", columnNames);
    }

    /// <summary>
    ///     Generates an INSERT clause from the given column mappings.
    /// </summary>
    /// <param name="mappings">The column mappings.</param>
    /// <returns>The INSERT clause as a string.</returns>
    public string GetInsertClause(IEnumerable<ColumnMapping> mappings) {
        var mappingsList = mappings?.ToList() ?? throw new ArgumentNullException(nameof(mappings));

        if (mappingsList.Count == 0) {
            return string.Empty;
        }

        var columnNames = mappingsList.Select(m => $"[{m.DestinationColumn}]");
        return string.Join(", ", columnNames);
    }
}