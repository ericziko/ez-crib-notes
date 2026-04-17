using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="SeedSourceCustomersCommand"/> via <see cref="EtlService.SeedCustomers"/>.
/// </summary>
[Collection("SharedSqlServer collection")]
public class SeedSourceCustomersCommandTests
{
    private readonly SharedSqlServerFixture _fixture;
    private readonly IConfiguration _configuration;

    public SeedSourceCustomersCommandTests(SharedSqlServerFixture fixture)
    {
        _fixture = fixture;
        _configuration = BuildConfiguration();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_Succeeds()
    {
        // Arrange: truncate first (shared fixture)
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(conn, "Customer");
        await conn.CloseAsync();

        var service = BuildEtlService();

        // Act & Assert: should not throw
        var exception = await Record.ExceptionAsync(() => service.SeedCustomers(50));
        Assert.Null(exception);
    }

    [Fact]
    public async Task Test_ExecuteAsync_CreatesNRows()
    {
        // Arrange: truncate first (shared fixture)
        await using var connSetup = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(connSetup, "Customer");
        await connSetup.CloseAsync();

        var service = BuildEtlService();

        // Act
        await service.SeedCustomers(50);

        // Assert: verify exactly 50 rows in TestDbSource.dbo.Customer
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        var count = await TestDatabaseHelper.GetRowCountAsync(conn, "Customer");
        Assert.Equal(50, count);
    }

    [Fact]
    public async Task Test_ExecuteAsync_WithZeroCount_Succeeds()
    {
        // Arrange: truncate first to eliminate leftover rows from shared fixture
        await using var setupConn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(setupConn, "Customer");
        await setupConn.CloseAsync();

        var service = BuildEtlService();

        // Act
        await service.SeedCustomers(0);

        // Assert: 0 rows inserted, no exception
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        var count = await TestDatabaseHelper.GetRowCountAsync(conn, "Customer");
        Assert.Equal(0, count);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private EtlService BuildEtlService()
        => new(_configuration, NullLogger<EtlService>.Instance);

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
