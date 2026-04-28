namespace Dapper.ETL.Library;

/// <summary>
/// Configuration options for ETL services, supporting multi-connection routing.
/// </summary>
public class EtlOptions {
    /// <summary>
    /// Connection string for the source database (read operations).
    /// </summary>
    public string SourceConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string for the target database (write operations).
    /// Reserved for future orchestrator use.
    /// </summary>
    public string TargetConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string for the logs database (audit/logging operations).
    /// Reserved for future orchestrator use.
    /// </summary>
    public string LogsConnectionString { get; set; } = string.Empty;
}