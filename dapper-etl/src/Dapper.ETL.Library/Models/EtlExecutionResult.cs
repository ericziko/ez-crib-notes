using System.Collections.Generic;
using System.Linq;

namespace Dapper.ETL.Library.Models;

/// <summary>
/// Represents the result of an ETL execution.
/// </summary>
public class EtlExecutionResult {
    /// <summary>
    /// Initializes a new instance of the <see cref="EtlExecutionResult" /> class.
    /// </summary>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="tableCopyResults">The results of table copy operations.</param>
    /// <param name="storedProcedureResults">The results of stored procedure executions.</param>
    /// <param name="errorMessage">An error message if the execution failed.</param>
    public EtlExecutionResult(
        bool success,
        IEnumerable<TableCopyResult>? tableCopyResults = null,
        IEnumerable<StoredProcedureResult>? storedProcedureResults = null,
        string? errorMessage = null) {
        Success = success;
        TableCopyResults = tableCopyResults?.ToList() ?? new List<TableCopyResult>();
        StoredProcedureResults = storedProcedureResults?.ToList() ?? new List<StoredProcedureResult>();
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the results of table copy operations.
    /// </summary>
    public IList<TableCopyResult> TableCopyResults { get; }

    /// <summary>
    /// Gets the results of stored procedure executions.
    /// </summary>
    public IList<StoredProcedureResult> StoredProcedureResults { get; }

    /// <summary>
    /// Gets an error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; }
}