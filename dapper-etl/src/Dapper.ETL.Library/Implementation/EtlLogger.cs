using System;
using Dapper.ETL.Library.Interfaces;

namespace Dapper.ETL.Library.Implementation;

/// <summary>
/// Basic console-based implementation of ETL logging.
/// </summary>
public class EtlLogger : IEtlLogger {
    /// <summary>
    /// Logs the start of a table copy operation.
    /// </summary>
    /// <param name="tableName">The name of the table being copied.</param>
    /// <param name="rowCount">The number of rows to copy.</param>
    public void LogTableCopyStarted(string tableName, int rowCount) {
        Console.WriteLine($"[ETL] Starting table copy: {tableName} ({rowCount} rows)");
    }

    /// <summary>
    /// Logs the completion of a table copy operation.
    /// </summary>
    /// <param name="tableName">The name of the table that was copied.</param>
    /// <param name="rowCount">The number of rows copied.</param>
    /// <param name="durationMs">The duration of the operation in milliseconds.</param>
    public void LogTableCopyCompleted(string tableName, int rowCount, long durationMs) {
        Console.WriteLine($"[ETL] Completed table copy: {tableName} ({rowCount} rows in {durationMs}ms)");
    }

    /// <summary>
    /// Logs that a table was truncated.
    /// </summary>
    /// <param name="tableName">The name of the table that was truncated.</param>
    public void LogTableTruncated(string tableName) {
        Console.WriteLine($"[ETL] Truncated table: {tableName}");
    }

    /// <summary>
    /// Logs the execution of a stored procedure.
    /// </summary>
    /// <param name="procedureName">The name of the stored procedure.</param>
    /// <param name="rowsAffected">The number of rows affected by the procedure.</param>
    public void LogStoredProcedureExecuted(string procedureName, int rowsAffected) {
        Console.WriteLine($"[ETL] Executed stored procedure: {procedureName} ({rowsAffected} rows affected)");
    }

    /// <summary>
    /// Logs the processing of a batch of records.
    /// </summary>
    /// <param name="batchNumber">The batch number.</param>
    /// <param name="rowCount">The number of rows in the batch.</param>
    public void LogBatchProcessed(int batchNumber, int rowCount) {
        Console.WriteLine($"[ETL] Processed batch {batchNumber} ({rowCount} rows)");
    }

    /// <summary>
    /// Logs an error that occurred during ETL operations.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    public void LogError(string errorMessage, Exception exception) {
        Console.WriteLine($"[ETL ERROR] {errorMessage}");
        Console.WriteLine($"[ETL ERROR] Exception: {exception.Message}");
    }
}