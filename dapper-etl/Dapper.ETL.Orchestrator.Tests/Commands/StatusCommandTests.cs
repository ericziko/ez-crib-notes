using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="StatusCommand"/> via <see cref="DataService"/>.
/// </summary>
public class StatusCommandTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture = new();
    private IConfiguration _configuration = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _configuration = BuildConfiguration();
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_DisplaysRowCounts()
    {
        // Arrange: seed 3 source rows
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
        // Arrange: no seed data — database starts empty after fixture initialises
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
