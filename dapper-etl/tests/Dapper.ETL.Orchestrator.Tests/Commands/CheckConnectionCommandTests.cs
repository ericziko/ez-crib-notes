using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Data.SqlClient;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for the check-connection operation.
/// Tests exercise SQL connectivity directly because <see cref="CheckConnectionCommand" />
/// renders to AnsiConsole which is not suitable for headless test execution.
/// </summary>
[Collection("SharedSqlServer collection")]
public class CheckConnectionCommandTests {
    private readonly SharedSqlServerFixture _fixture;

    public CheckConnectionCommandTests(SharedSqlServerFixture fixture) {
        _fixture = fixture;
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_AllConnectionsValid_Succeeds() {
        // Arrange / Act: open and exercise all three databases
        await using var sourceConn = await _fixture.GetConnectionAsync("TestDbSource");
        await using var targetConn = await _fixture.GetConnectionAsync("TestDbTarget");
        await using var logsConn = await _fixture.GetConnectionAsync("EtlLogs");

        var sourceServer = await ExecuteScalarAsync(sourceConn, "SELECT @@SERVERNAME");
        var targetServer = await ExecuteScalarAsync(targetConn, "SELECT @@SERVERNAME");
        var logsServer = await ExecuteScalarAsync(logsConn, "SELECT @@SERVERNAME");

        // Assert: all connections returned a server name
        Assert.False(string.IsNullOrWhiteSpace(sourceServer), "Source server name should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(targetServer), "Target server name should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(logsServer), "Logs server name should not be empty.");
    }

    [Fact]
    public async Task Test_ExecuteAsync_InvalidSource_ReportsFail() {
        // Arrange: use a bogus connection string that will time out quickly
        const string badConnectionString =
            "Server=invalid-host-that-does-not-exist,1433;Database=X;" +
            "User Id=sa;Password=bad!;TrustServerCertificate=True;Connect Timeout=2;";

        // Act & Assert: connecting to the bad host must throw
        var exception = await Record.ExceptionAsync(async () => {
            await using var conn = new SqlConnection(badConnectionString);
            await conn.OpenAsync();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task Test_ExecuteAsync_InvalidSeq_ReportsFail() {
        // Arrange: source and target are valid; simulate a missing Logs/Seq
        // connection by using a non-existent database name on the live container.
        var badLogsCs = _fixture.GetConnectionString("DoesNotExistDb");

        // Act & Assert: connecting to the missing database must throw
        var exception = await Record.ExceptionAsync(async () => {
            await using var conn = new SqlConnection(badLogsCs);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @@SERVERNAME";
            await cmd.ExecuteScalarAsync();
        });

        Assert.NotNull(exception);

        // Source and Target remain reachable
        await using var sourceConn = await _fixture.GetConnectionAsync("TestDbSource");
        var serverName = await ExecuteScalarAsync(sourceConn, "SELECT @@SERVERNAME");
        Assert.False(string.IsNullOrWhiteSpace(serverName));
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static async Task<string?> ExecuteScalarAsync(SqlConnection conn, string sql) {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return (await cmd.ExecuteScalarAsync())?.ToString();
    }
}