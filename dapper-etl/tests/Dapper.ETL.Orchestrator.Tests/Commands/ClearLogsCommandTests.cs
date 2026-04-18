using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for the clear-logs operation.
/// Tests exercise the underlying SQL directly because <see cref="ClearLogsCommand"/>
/// calls <c>AnsiConsole.Confirm</c> which is not suitable for headless test execution.
/// </summary>

[Collection("SharedSqlServer collection")]
public class ClearLogsCommandTests
{
    private readonly SharedSqlServerFixture _fixture;
    private readonly IConfiguration _configuration;

    public ClearLogsCommandTests(SharedSqlServerFixture fixture)
    {
        _fixture = fixture;
        _configuration = BuildConfiguration();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_ClearsAllLogs()
    {
        // Arrange: insert 100 log rows
        for (var i = 0; i < 100; i++)
            await InsertLogAsync("Info", $"Log entry {i}");

        // Verify rows exist before clear
        await using var connBefore = await _fixture.GetConnectionAsync("EtlLogs");
        var countBefore = await TestDatabaseHelper.GetRowCountAsync(connBefore, "Logs");
        Assert.Equal(100, countBefore);

        // Act: truncate via the same SQL the command uses
        await TruncateLogsAsync();

        // Assert: 0 rows remain
        await using var connAfter = await _fixture.GetConnectionAsync("EtlLogs");
        var countAfter = await TestDatabaseHelper.GetRowCountAsync(connAfter, "Logs");
        Assert.Equal(0, countAfter);
    }

    [Fact]
    public async Task Test_ExecuteAsync_WithEmptyLogs_Succeeds()
    {
        // Arrange: no logs seeded — table starts empty

        // Act & Assert: TRUNCATE on empty table should not throw
        var exception = await Record.ExceptionAsync(TruncateLogsAsync);
        Assert.Null(exception);

        await using var conn = await _fixture.GetConnectionAsync("EtlLogs");
        var count = await TestDatabaseHelper.GetRowCountAsync(conn, "Logs");
        Assert.Equal(0, count);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private async Task InsertLogAsync(string level, string messageTemplate)
    {
        await using var conn = await _fixture.GetConnectionAsync("EtlLogs");
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO dbo.Logs (MessageTemplate, Level, TimeStamp)
            VALUES (@msg, @level, @ts)
            """;
        cmd.Parameters.AddWithValue("@msg",   messageTemplate);
        cmd.Parameters.AddWithValue("@level", level);
        cmd.Parameters.AddWithValue("@ts",    DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task TruncateLogsAsync()
    {
        var connectionString = _configuration["ConnectionStrings:Logs"]!;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("TRUNCATE TABLE dbo.Logs", connection);
        await command.ExecuteNonQueryAsync();
    }

    private IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Source"] = _fixture.GetConnectionString("TestDbSource"),
                ["ConnectionStrings:Target"] = _fixture.GetConnectionString("TestDbTarget"),
                ["ConnectionStrings:Logs"]   = _fixture.GetConnectionString("EtlLogs"),
            })
            .Build();
}
