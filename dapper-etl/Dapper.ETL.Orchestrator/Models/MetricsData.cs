namespace Dapper.ETL.Orchestrator.Models
{
    /// <summary>
    /// Represents collected ETL metrics data for export or reporting.
    /// </summary>
    public class MetricsData
    {
        /// <summary>
        /// Gets or sets the timestamp when metrics were captured.
        /// </summary>
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the total rows copied in the last ETL run.
        /// </summary>
        public long TotalRowsCopied { get; set; }

        /// <summary>
        /// Gets or sets the duration of the last ETL run in milliseconds.
        /// </summary>
        public long LastRunDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the throughput in rows per second.
        /// </summary>
        public double ThroughputRowsPerSec { get; set; }

        /// <summary>
        /// Gets or sets the total number of errors recorded.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of validations passed.
        /// </summary>
        public long ValidationsPassed { get; set; }

        /// <summary>
        /// Gets or sets the total number of validations failed.
        /// </summary>
        public long ValidationsFailed { get; set; }
    }
}
