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

    [Fact]
    public async Task Test_SeedCustomers_ReturnsCorrectInsertedCount()
    {
        // Arrange
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(conn, "Customer");
        await conn.CloseAsync();

        var service = BuildEtlService();

        // Act
        var count = await service.SeedCustomers(5);

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task Test_SeedCustomers_GeneratesExpectedEmailFormat()
    {
        // Arrange
        await using var setupConn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(setupConn, "Customer");
        await setupConn.CloseAsync();

        var service = BuildEtlService();
        await service.SeedCustomers(1);

        // Act
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 EmailAddress FROM dbo.Customer ORDER BY CustomerId";
        var email = (string?)await cmd.ExecuteScalarAsync();

        // Assert
        Assert.Equal("john.smith1@example.com", email);
    }

    [Fact]
    public async Task Test_SeedCustomers_MissingConnectionString_Throws()
    {
        // Arrange
        var badConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Target"] = _fixture.GetConnectionString("TestDbTarget"),
                ["ConnectionStrings:Logs"]   = _fixture.GetConnectionString("EtlLogs"),
            })
            .Build();
        var etlService = new EtlService(badConfig, NullLogger<EtlService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => etlService.SeedCustomers(1));
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
