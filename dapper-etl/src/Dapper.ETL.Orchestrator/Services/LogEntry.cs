namespace Dapper.ETL.Orchestrator.Services;

/// <summary>
/// Represents a single log entry from EtlLogs.dbo.Logs.
/// </summary>
public record LogEntry(
    DateTime TimeStamp,
    string Level,
    string MessageTemplate,
    string? Properties);