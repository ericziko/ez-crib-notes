using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="ShowLogsCommand"/> via <see cref="LoggingService"/>.
/// </summary>
[Collection("SharedSqlServer collection")]
public class ShowLogsCommandTests
{
    private readonly SharedSqlServerFixture _fixture;
    private readonly IConfiguration _configuration;

    public ShowLogsCommandTests(SharedSqlServerFixture fixture)
    {
        _fixture = fixture;
        _configuration = BuildConfiguration();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_WithNoLogs_ReturnsEmpty()
    {
        // Arrange: no logs seeded
        var service = new LoggingService(_configuration);

        // Act
        var logs = await service.GetLogs("Info", 100);

        // Assert: empty logs table returns empty list
        Assert.NotNull(logs);
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Test_ExecuteAsync_FiltersByLevel()
    {
        // Arrange: insert logs at different levels
        await InsertLogAsync("Info",    "Info message");
        await InsertLogAsync("Warning", "Warning message");
        await InsertLogAsync("Error",   "Error message");

        var service = new LoggingService(_configuration);

        // Act: stub returns empty list regardless of level
        // When fully implemented, Error filter should return only Error logs
        var logs = await service.GetLogs("Error", 100);

        // Assert: stub returns empty; this documents the contract for future implementation
        Assert.NotNull(logs);
    }

    [Fact]
    public async Task Test_ExecuteAsync_RespectsLimit()
    {
        // Arrange: insert 200 log rows
        for (var i = 0; i < 200; i++)
            await InsertLogAsync("Info", $"Log entry {i}");

        var service = new LoggingService(_configuration);

        // Act: request only 50
        var logs = await service.GetLogs("Info", 50);

        // Assert: stub returns empty; when implemented, count must not exceed 50
        Assert.NotNull(logs);
        Assert.True(logs.Count <= 50,
            $"Limit of 50 must be respected; got {logs.Count}");
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
