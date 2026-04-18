namespace Dapper.ETL.Orchestrator.Models;

/// <summary>
/// Metadata about the last ETL run. Populated in Phase 4.5.
/// </summary>
public class EtlRunInfo
{
    public DateTime RunAt { get; set; }
    public long DurationMs { get; set; }
    public long RowsCopied { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
