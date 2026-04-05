namespace Dapper.ETL.Orchestrator.Services
{
    using System.Diagnostics.Metrics;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service for recording OpenTelemetry metrics during ETL operations.
    /// </summary>
    public class MetricsService
    {
        private readonly Counter<long> _rowsCopiedCounter;
        private readonly Counter<long> _rowsExpectedCounter;
        private readonly Histogram<long> _durationHistogram;
        private readonly Counter<long> _errorCounter;
        private readonly Counter<long> _validationPassedCounter;
        private readonly Counter<long> _validationFailedCounter;
        private readonly ILogger<MetricsService>? _logger;

        private double _throughput;

    private Dictionary<string, object>? _lastMetrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsService"/> class.
        /// </summary>
        /// <param name="meter">The OpenTelemetry meter (name: "Dapper.ETL").</param>
        /// <param name="logger">Optional logger for debug output.</param>
        public MetricsService(Meter meter, ILogger<MetricsService>? logger = null)
        {
            if (meter == null)
            {
                throw new ArgumentNullException(nameof(meter));
            }

            _logger = logger;

            _rowsCopiedCounter = meter.CreateCounter<long>(
                "etl.rows_copied",
                description: "Rows copied per table");

            _rowsExpectedCounter = meter.CreateCounter<long>(
                "etl.rows_expected",
                description: "Rows expected per table");

            _durationHistogram = meter.CreateHistogram<long>(
                "etl.duration_ms",
                description: "ETL duration in milliseconds");

            meter.CreateObservableGauge<double>(
                "etl.throughput_rows_per_sec",
                () => new[] { new Measurement<double>(_throughput) });

            _errorCounter = meter.CreateCounter<long>(
                "etl.errors",
                description: "Errors by type");

            _validationPassedCounter = meter.CreateCounter<long>(
                "etl.validations_passed");

            _validationFailedCounter = meter.CreateCounter<long>(
                "etl.validations_failed");
        }

        /// <summary>
        /// Records the number of rows copied for a table.
        /// </summary>
        public void RecordRowsCopied(long count, string tableName)
        {
            _rowsCopiedCounter.Add(count, new KeyValuePair<string, object?>("table", tableName));
            _logger?.LogDebug("Rows copied: {Count} for table {TableName}", count, tableName);
        }

        /// <summary>
        /// Records the number of rows expected for a table.
        /// </summary>
        public void RecordRowsExpected(long count, string tableName)
        {
            _rowsExpectedCounter.Add(count, new KeyValuePair<string, object?>("table", tableName));
            _logger?.LogDebug("Rows expected: {Count} for table {TableName}", count, tableName);
        }

        /// <summary>
        /// Records the duration of an ETL stage in milliseconds.
        /// </summary>
        public void RecordDuration(long durationMs, string stage = "total")
        {
            _durationHistogram.Record(durationMs, new KeyValuePair<string, object?>("stage", stage));
            _logger?.LogDebug("Duration recorded: {DurationMs}ms for stage {Stage}", durationMs, stage);
        }

        /// <summary>
        /// Records an error by type.
        /// </summary>
        public void RecordError(string errorType)
        {
            _errorCounter.Add(1, new KeyValuePair<string, object?>("error_type", errorType));
            _logger?.LogDebug("Error recorded: {ErrorType}", errorType);
        }

        /// <summary>
        /// Records a passed validation.
        /// </summary>
        public void RecordValidationPassed()
        {
            _validationPassedCounter.Add(1);
            _logger?.LogDebug("Validation passed");
        }

        /// <summary>
        /// Records a failed validation.
        /// </summary>
        public void RecordValidationFailed()
        {
            _validationFailedCounter.Add(1);
            _logger?.LogDebug("Validation failed");
        }

        /// <summary>
        /// Updates the current throughput value (rows per second) used by the observable gauge.
        /// </summary>
        public void UpdateThroughput(double rowsPerSec)
        {
            _throughput = rowsPerSec;
            _logger?.LogDebug("Throughput updated: {RowsPerSec} rows/sec", rowsPerSec);
        }

        /// <summary>
        /// Caches a metrics snapshot from the last ETL run for later retrieval by commands.
        /// </summary>
        public void StoreMetrics(Dictionary<string, object> metrics)
        {
            _lastMetrics = new Dictionary<string, object>(metrics);
        }

        /// <summary>
        /// Returns the metrics cached by the last call to <see cref="StoreMetrics"/>, or null if no run has been recorded.
        /// </summary>
        public Dictionary<string, object>? GetLastMetrics()
        {
            return _lastMetrics;
        }
    }
}
