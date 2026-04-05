namespace Dapper.ETL.Orchestrator.Services;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Represents a single log entry from EtlLogs.dbo.Logs.
/// </summary>
public record LogEntry(
    DateTime TimeStamp,
    string Level,
    string MessageTemplate,
    string? Properties);

/// <summary>
/// Service for querying structured logs from the EtlLogs database.
/// Stub implementation - full logic added in Phase 4.3+.
/// </summary>
public class LoggingService
{
    private readonly IConfiguration _configuration;

    public LoggingService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Retrieves log entries filtered by minimum level and limited to the specified row count.
    /// Queries EtlLogs.dbo.Logs ordered by TimeStamp descending.
    /// </summary>
    /// <param name="level">Minimum log level: Info, Warning, or Error.</param>
    /// <param name="limit">Maximum number of rows to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<List<LogEntry>> GetLogs(
        string level,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Stub: returns empty list - Phase 4.3 will query EtlLogs.dbo.Logs via Dapper
        return Task.FromResult(new List<LogEntry>());
    }
}
