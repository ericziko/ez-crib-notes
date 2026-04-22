using Dapper.ETL.Orchestrator.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Dapper.ETL.Orchestrator.Services;

/// <summary>
///     Provides data access helpers for ETL status and maintenance operations.
/// </summary>
public class DataService {
    private readonly string _logsConnectionString;
    private readonly string _sourceConnectionString;
    private readonly string _targetConnectionString;

    public DataService(
        [FromKeyedServices("Source")] string sourceConnectionString,
        [FromKeyedServices("Target")] string targetConnectionString,
        [FromKeyedServices("Logs")] string logsConnectionString) {
        _sourceConnectionString = sourceConnectionString;
        _targetConnectionString = targetConnectionString;
        _logsConnectionString = logsConnectionString;
    }

    /// <summary>
    ///     Returns the row count for a table in the given database using the appropriate connection string.
    /// </summary>
    /// <param name="database">Logical database name: "Source", "Target", or "Logs".</param>
    /// <param name="table">Fully-qualified table name, e.g. "dbo.Customer".</param>
    public async Task<int> GetRowCount(string database, string table) {
        var connectionString = database switch {
            "Source" => _sourceConnectionString,
            "Target" => _targetConnectionString,
            "Logs" => _logsConnectionString,
            _ => throw new ArgumentException($"Unknown database '{database}'. Use Source, Target, or Logs.", nameof(database))
        };

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT COUNT_BIG(1) FROM {table}", connection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    ///     Truncates all target tables and resets their sequences.
    /// </summary>
    public async Task ResetTargetDatabase() {
        var tables = new[] {
            "dbo.CustomerLoyaltyRewards",
            "dbo.CustomerEmailList",
            "dbo.CustomerCopy"
        };

        await using var connection = new SqlConnection(_targetConnectionString);
        await connection.OpenAsync();

        foreach (var table in tables) {
            await using var truncateCmd = new SqlCommand($"TRUNCATE TABLE {table}", connection);
            await truncateCmd.ExecuteNonQueryAsync();
        }

        // Reset sequences so the next insert starts at 1
        await using var resetEmailSeq = new SqlCommand(
            "ALTER SEQUENCE dbo.CustomerEmailIdSequence RESTART WITH 1", connection);
        await resetEmailSeq.ExecuteNonQueryAsync();

        await using var resetLoyaltySeq = new SqlCommand(
            "ALTER SEQUENCE dbo.LoyaltyRewardIdSequence RESTART WITH 1", connection);
        await resetLoyaltySeq.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Returns information about the last ETL run. Returns null until Phase 4.5 wires up metrics persistence.
    /// </summary>
    public Task<EtlRunInfo?> GetLastEtlRunInfo() {
        // Placeholder: populated in Phase 4.5 when metrics cache/database is available.
        return Task.FromResult<EtlRunInfo?>(null);
    }
}