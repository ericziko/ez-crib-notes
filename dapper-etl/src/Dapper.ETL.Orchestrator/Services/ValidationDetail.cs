namespace Dapper.ETL.Orchestrator.Services;

/// <summary>
/// Detail for a single table's validation result.
/// </summary>
public record ValidationDetail(
    string TableName,
    long SourceCount,
    long TargetCount,
    double MatchPercent,
    string Status);