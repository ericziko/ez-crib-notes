using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Dapper.ETL.Orchestrator.Tests.Utility;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Integration;

/// <summary>
/// End-to-end workflow integration tests.
/// Each test class owns its own SqlServerFixture so containers are isolated.
/// </summary>
public class EndToEndTests : IAsyncLifetime
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
    public async Task Test_SeedValidateCompareStatus_FullWorkflow()
    {
        // Step 1 – Seed
        var etlService = new EtlService(_configuration, NullLogger<EtlService>.Instance);
        var seeded = await etlService.SeedCustomers(10);
        Assert.Equal(10, seeded);

        // Step 2 – Validate (stub always returns success)
        var validationService = new ValidationService(_configuration);
        var (validateSuccess, validateResults) = await validationService.ValidateQuick();
        Assert.True(validateSuccess);
        Assert.NotNull(validateResults);

        // Step 3 – Compare: verify source rows are present
        await using var sourceConn = await _fixture.GetConnectionAsync("TestDbSource");
        var sourceCount = await TestDatabaseHelper.GetRowCountAsync(sourceConn, "Customer");
        Assert.Equal(10, sourceCount);

        // Step 4 – Status: verify target tables are accessible
        var dataService = new DataService(_configuration);
        var targetCopyCount   = await dataService.GetRowCount("Target", "dbo.CustomerCopy");
        var targetEmailCount  = await dataService.GetRowCount("Target", "dbo.CustomerEmailList");
        var targetLoyalCount  = await dataService.GetRowCount("Target", "dbo.CustomerLoyaltyRewards");

        // Targets start empty (ETL stub does not copy rows yet)
        Assert.Equal(0, targetCopyCount);
        Assert.Equal(0, targetEmailCount);
        Assert.Equal(0, targetLoyalCount);
    }

    [Fact]
    public async Task Test_ETL_ClearLogs_CheckConnection()
    {
        // Step 1 – Run ETL (stub returns success with 0 rows)
        var etlService = new EtlService(_configuration, NullLogger<EtlService>.Instance);
        var etlResult = await etlService.RunEtl(Dapper.ETL.Library.Models.EtlTransactionMode.Atomic);
        Assert.True(etlResult.Success);

        // Step 2 – Seed some log rows then clear them
        for (var i = 0; i < 5; i++)
            await InsertLogAsync("Info", $"ETL log entry {i}");

        await using var logConnBefore = await _fixture.GetConnectionAsync("EtlLogs");
        var logCountBefore = await TestDatabaseHelper.GetRowCountAsync(logConnBefore, "Logs");
        Assert.Equal(5, logCountBefore);

        // Clear logs
        await TruncateLogsAsync();

        await using var logConnAfter = await _fixture.GetConnectionAsync("EtlLogs");
        var logCountAfter = await TestDatabaseHelper.GetRowCountAsync(logConnAfter, "Logs");
        Assert.Equal(0, logCountAfter);

        // Step 3 – Check connection: all three databases reachable
        await TestHelpers.AssertAllConnectionsReachableAsync(
            _fixture.GetConnectionString("TestDbSource"),
            _fixture.GetConnectionString("TestDbTarget"),
            _fixture.GetConnectionString("EtlLogs"));
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
        var cs = _configuration["ConnectionStrings:Logs"]!;
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("TRUNCATE TABLE dbo.Logs", conn);
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
