using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Dapper.ETL.Orchestrator.Tests.Utility;

/// <summary>
/// Common assertion helpers shared across integration and end-to-end tests.
/// </summary>
public static class TestHelpers {
    /// <summary>
    /// Asserts that the row count in <paramref name="sourceTable" /> (source DB)
    /// matches the row count in each of <paramref name="targetTables" /> (target DB).
    /// </summary>
    public static async Task AssertRowCountsMatchAsync(
        string sourceConnectionString,
        string sourceTable,
        string targetConnectionString,
        params string[] targetTables) {
        await using var sourceConn = new SqlConnection(sourceConnectionString);
        await sourceConn.OpenAsync();
        var sourceCount = await GetRowCountAsync(sourceConn, sourceTable);

        await using var targetConn = new SqlConnection(targetConnectionString);
        await targetConn.OpenAsync();

        foreach (var table in targetTables) {
            var targetCount = await GetRowCountAsync(targetConn, table);
            Assert.Equal(sourceCount, targetCount);
        }
    }

    /// <summary>
    /// Asserts that the file at <paramref name="filePath" /> exists and contains
    /// valid content for the specified <paramref name="format" /> ("json" or "csv").
    /// </summary>
    public static void AssertLogsExportValid(string filePath, string format) {
        Assert.True(File.Exists(filePath), $"Export file '{filePath}' must exist.");

        var content = File.ReadAllText(filePath);
        Assert.False(string.IsNullOrWhiteSpace(content), "Export file must not be empty.");

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase)) {
            var parsed = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal(JsonValueKind.Array, parsed.ValueKind);
        }
        else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase)) {
            var lines = File.ReadAllLines(filePath);
            Assert.True(lines.Length >= 1, "CSV export must contain at least a header row.");
        }
        else {
            throw new ArgumentException($"Unknown format '{format}'. Expected 'json' or 'csv'.");
        }
    }

    /// <summary>
    /// Asserts that the metrics export file at <paramref name="filePath" /> exists
    /// and is valid for the specified <paramref name="format" /> ("json" or "csv").
    /// </summary>
    public static void AssertMetricsExportValid(string filePath, string format) {
        Assert.True(File.Exists(filePath), $"Metrics export file '{filePath}' must exist.");

        var content = File.ReadAllText(filePath);
        Assert.False(string.IsNullOrWhiteSpace(content), "Metrics export file must not be empty.");

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase)) {
            var parsed = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal(JsonValueKind.Object, parsed.ValueKind);
        }
        else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase)) {
            var lines = File.ReadAllLines(filePath);
            Assert.True(lines.Length >= 1, "CSV metrics export must contain at least a header row.");
            Assert.Equal("Metric,Value", lines[0]);
        }
        else {
            throw new ArgumentException($"Unknown format '{format}'. Expected 'json' or 'csv'.");
        }
    }

    /// <summary>
    /// Asserts that all provided connection strings can open a SQL connection successfully.
    /// </summary>
    public static async Task AssertAllConnectionsReachableAsync(params string[] connectionStrings) {
        foreach (var cs in connectionStrings) {
            Assert.False(string.IsNullOrWhiteSpace(cs), "Connection string must not be empty.");
            var exception = await Record.ExceptionAsync(async () => {
                await using var conn = new SqlConnection(cs);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync();
            });
            Assert.Null(exception);
        }
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private static async Task<int> GetRowCountAsync(SqlConnection conn, string tableExpression) {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableExpression}";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}