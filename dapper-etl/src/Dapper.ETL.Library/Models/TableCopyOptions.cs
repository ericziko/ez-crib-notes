namespace Dapper.ETL.Library.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Options for a table copy operation.
    /// </summary>
    public class TableCopyOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableCopyOptions"/> class.
        /// </summary>
        /// <param name="truncateDestination">Whether to truncate the destination table before copying.</param>
        /// <param name="batchSize">The size of batches to process.</param>
        /// <param name="columnMappingOverrides">Optional column mapping overrides.</param>
        public TableCopyOptions(
            bool truncateDestination = true,
            int batchSize = 1000,
            IDictionary<string, string>? columnMappingOverrides = null)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
            }

            TruncateDestination = truncateDestination;
            BatchSize = batchSize;
            ColumnMappingOverrides = columnMappingOverrides ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets a value indicating whether the destination table should be truncated before copying.
        /// </summary>
        public bool TruncateDestination { get; }

        /// <summary>
        /// Gets the size of batches to process.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// Gets the optional column mapping overrides.
        /// </summary>
        public IDictionary<string, string> ColumnMappingOverrides { get; }
    }
}
