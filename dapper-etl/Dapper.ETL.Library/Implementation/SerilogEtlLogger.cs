namespace Dapper.ETL.Library.Implementation
{
    using System;
    using Dapper.ETL.Library.Interfaces;
    using Serilog;

    /// <summary>
    /// Serilog-backed implementation of ETL logging.
    /// </summary>
    public class SerilogEtlLogger : IEtlLogger
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="SerilogEtlLogger"/> using the static Serilog logger.
        /// </summary>
        public SerilogEtlLogger()
        {
            _logger = Log.ForContext<SerilogEtlLogger>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SerilogEtlLogger"/> with an explicit logger instance.
        /// </summary>
        /// <param name="logger">The Serilog logger instance to use.</param>
        public SerilogEtlLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void LogTableCopyStarted(string tableName, int rowCount)
        {
            _logger.Information("Starting table copy: {TableName} ({RowCount} rows)", tableName, rowCount);
        }

        /// <inheritdoc />
        public void LogTableCopyCompleted(string tableName, int rowCount, long durationMs)
        {
            _logger.Information(
                "Completed table copy: {TableName} ({RowCount} rows in {DurationMs}ms)",
                tableName, rowCount, durationMs);
        }

        /// <inheritdoc />
        public void LogTableTruncated(string tableName)
        {
            _logger.Information("Truncated table: {TableName}", tableName);
        }

        /// <inheritdoc />
        public void LogStoredProcedureExecuted(string procedureName, int rowsAffected)
        {
            _logger.Information(
                "Executed stored procedure: {ProcedureName} ({RowsAffected} rows affected)",
                procedureName, rowsAffected);
        }

        /// <inheritdoc />
        public void LogBatchProcessed(int batchNumber, int rowCount)
        {
            _logger.Information("Processed batch {BatchNumber} ({RowCount} rows)", batchNumber, rowCount);
        }

        /// <inheritdoc />
        public void LogError(string errorMessage, Exception exception)
        {
            _logger.Error(exception, "ETL error: {ErrorMessage}", errorMessage);
        }
    }
}
