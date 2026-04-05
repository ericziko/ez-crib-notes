namespace Dapper.ETL.Library.Models
{
    using System;

    /// <summary>
    /// Represents the result of a table copy operation.
    /// </summary>
    public class TableCopyResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableCopyResult"/> class.
        /// </summary>
        /// <param name="success">Whether the copy operation was successful.</param>
        /// <param name="sourceTable">The name of the source table.</param>
        /// <param name="destinationTable">The name of the destination table.</param>
        /// <param name="rowCount">The number of rows copied.</param>
        /// <param name="durationMs">The duration of the operation in milliseconds.</param>
        /// <param name="errorMessage">An error message if the operation failed.</param>
        public TableCopyResult(
            bool success,
            string sourceTable,
            string destinationTable,
            int rowCount,
            long durationMs,
            string? errorMessage = null)
        {
            Success = success;
            SourceTable = sourceTable ?? throw new ArgumentNullException(nameof(sourceTable));
            DestinationTable = destinationTable ?? throw new ArgumentNullException(nameof(destinationTable));
            RowCount = rowCount;
            DurationMs = durationMs;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets a value indicating whether the copy operation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the name of the source table.
        /// </summary>
        public string SourceTable { get; }

        /// <summary>
        /// Gets the name of the destination table.
        /// </summary>
        public string DestinationTable { get; }

        /// <summary>
        /// Gets the number of rows copied.
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// Gets the duration of the operation in milliseconds.
        /// </summary>
        public long DurationMs { get; }

        /// <summary>
        /// Gets an error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; }
    }
}
