using System;

namespace Dapper.ETL.Library.Models;

/// <summary>
/// Represents the result of a stored procedure execution.
/// </summary>
public class StoredProcedureResult {
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedureResult" /> class.
    /// </summary>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="procedureName">The name of the stored procedure.</param>
    /// <param name="rowsAffected">The number of rows affected by the procedure.</param>
    /// <param name="errorMessage">An error message if the execution failed.</param>
    public StoredProcedureResult(
        bool success,
        string procedureName,
        int rowsAffected,
        string? errorMessage = null) {
        Success = success;
        ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
        RowsAffected = rowsAffected;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the name of the stored procedure.
    /// </summary>
    public string ProcedureName { get; }

    /// <summary>
    /// Gets the number of rows affected by the procedure.
    /// </summary>
    public int RowsAffected { get; }

    /// <summary>
    /// Gets an error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; }
}