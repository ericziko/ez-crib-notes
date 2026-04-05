namespace Dapper.ETL.Library.Interfaces
{
    using System;

    /// <summary>
    /// Interface for logging ETL operations and events.
    /// </summary>
    public interface IEtlLogger
    {
        /// <summary>
        /// Logs the start of a table copy operation.
        /// </summary>
        /// <param name="tableName">The name of the table being copied.</param>
        /// <param name="rowCount">The number of rows to copy.</param>
        void LogTableCopyStarted(string tableName, int rowCount);

        /// <summary>
        /// Logs the completion of a table copy operation.
        /// </summary>
        /// <param name="tableName">The name of the table that was copied.</param>
        /// <param name="rowCount">The number of rows copied.</param>
        /// <param name="durationMs">The duration of the operation in milliseconds.</param>
        void LogTableCopyCompleted(string tableName, int rowCount, long durationMs);

        /// <summary>
        /// Logs that a table was truncated.
        /// </summary>
        /// <param name="tableName">The name of the table that was truncated.</param>
        void LogTableTruncated(string tableName);

        /// <summary>
        /// Logs the execution of a stored procedure.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="rowsAffected">The number of rows affected by the procedure.</param>
        void LogStoredProcedureExecuted(string procedureName, int rowsAffected);

        /// <summary>
        /// Logs the processing of a batch of records.
        /// </summary>
        /// <param name="batchNumber">The batch number.</param>
        /// <param name="rowCount">The number of rows in the batch.</param>
        void LogBatchProcessed(int batchNumber, int rowCount);

        /// <summary>
        /// Logs an error that occurred during ETL operations.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        void LogError(string errorMessage, Exception exception);
    }
}
