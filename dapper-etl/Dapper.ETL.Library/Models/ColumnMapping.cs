namespace Dapper.ETL.Library.Models
{
    using System;

    /// <summary>
    /// Represents the mapping between a source column and a destination column.
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnMapping"/> class.
        /// </summary>
        /// <param name="sourceColumn">The name of the source column.</param>
        /// <param name="destinationColumn">The name of the destination column.</param>
        public ColumnMapping(string sourceColumn, string destinationColumn)
        {
            SourceColumn = sourceColumn ?? throw new ArgumentNullException(nameof(sourceColumn));
            DestinationColumn = destinationColumn ?? throw new ArgumentNullException(nameof(destinationColumn));
        }

        /// <summary>
        /// Gets the name of the source column.
        /// </summary>
        public string SourceColumn { get; }

        /// <summary>
        /// Gets the name of the destination column.
        /// </summary>
        public string DestinationColumn { get; }
    }
}
