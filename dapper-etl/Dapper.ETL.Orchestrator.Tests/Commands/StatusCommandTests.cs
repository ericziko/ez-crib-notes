using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="StatusCommand"/> via <see cref="DataService"/>.
/// </summary>
[Collection("SharedSqlServer collection")]
public class StatusCommandTests
{
    private readonly SharedSqlServerFixture _fixture;
    private readonly IConfiguration _configuration;

    public StatusCommandTests(SharedSqlServerFixture fixture)
    {
        _fixture = fixture;
        _configuration = BuildConfiguration();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_DisplaysRowCounts()
    {
        // Arrange: truncate first (shared fixture), then seed 3 source rows
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(conn, "Customer");
        await conn.CloseAsync();

        var etlService = new EtlService(_configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EtlService>.Instance);
        await etlService.SeedCustomers(3);

        var dataService = new DataService(_configuration);

        // Act
        var sourceCount = await dataService.GetRowCount("Source", "dbo.Customer");

        // Assert: 3 rows were seeded
        Assert.Equal(3, sourceCount);
    }

    [Fact]
    public async Task Test_ExecuteAsync_WithEmptyDatabase_ReturnsZeros()
    {
        // Arrange: truncate all tables — shared fixture may have rows from other tests
        await using var sourceConn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(sourceConn, "Customer");
        await sourceConn.CloseAsync();

        await using var targetConn = await _fixture.GetConnectionAsync("TestDbTarget");
        await TestDatabaseHelper.TruncateTableAsync(targetConn, "CustomerCopy");
        await TestDatabaseHelper.TruncateTableAsync(targetConn, "CustomerEmailList");
        await TestDatabaseHelper.TruncateTableAsync(targetConn, "CustomerLoyaltyRewards");
        await targetConn.CloseAsync();

        await using var logsConn = await _fixture.GetConnectionAsync("EtlLogs");
        await TestDatabaseHelper.TruncateTableAsync(logsConn, "Logs");
        await logsConn.CloseAsync();

        var dataService = new DataService(_configuration);

        // Act
        var sourceCount = await dataService.GetRowCount("Source", "dbo.Customer");
        var customerCopyCount = await dataService.GetRowCount("Target", "dbo.CustomerCopy");
        var emailListCount = await dataService.GetRowCount("Target", "dbo.CustomerEmailList");
        var loyaltyCount = await dataService.GetRowCount("Target", "dbo.CustomerLoyaltyRewards");
        var logsCount = await dataService.GetRowCount("Logs", "dbo.Logs");

        // Assert: all counts are zero before any ETL run
        Assert.Equal(0, sourceCount);
        Assert.Equal(0, customerCopyCount);
        Assert.Equal(0, emailListCount);
        Assert.Equal(0, loyaltyCount);
        Assert.Equal(0, logsCount);
    }

    [Fact]
    public async Task Test_GetRowCount_LogsDatabase_ReturnsCount()
    {
        // Arrange: ensure at least 3 log rows exist
        await using var logsConn = await _fixture.GetConnectionAsync("EtlLogs");
        await TestDatabaseHelper.TruncateTableAsync(logsConn, "Logs");
        for (int i = 0; i < 3; i++)
        {
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                "INSERT INTO [dbo].[Logs] ([MessageTemplate],[Level],[TimeStamp]) VALUES ('test','Info',GETUTCDATE())",
                logsConn);
            await cmd.ExecuteNonQueryAsync();
        }
        await logsConn.CloseAsync();

        var dataService = new DataService(_configuration);

        // Act
        var count = await dataService.GetRowCount("Logs", "dbo.Logs");

        // Assert
        Assert.True(count >= 3);
    }

    [Fact]
    public async Task Test_GetRowCount_InvalidDatabase_ThrowsArgumentException()
    {
        // Arrange
        var dataService = new DataService(_configuration);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => dataService.GetRowCount("BadDB", "dbo.Customer"));
    }

    [Fact]
    public async Task Test_GetLastEtlRunInfo_ReturnsNull()
    {
        // Arrange
        var dataService = new DataService(_configuration);

        // Act
        var result = await dataService.GetLastEtlRunInfo();

        // Assert
        Assert.Null(result);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

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
