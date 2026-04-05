using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="SeedSourceCustomersCommand"/> via <see cref="EtlService.SeedCustomers"/>.
/// </summary>
public class SeedSourceCustomersCommandTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture = new();
    private IConfiguration _configuration = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _configuration = BuildConfiguration();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_Succeeds()
    {
        // Arrange
        var service = BuildEtlService();

        // Act & Assert: should not throw
        var exception = await Record.ExceptionAsync(() => service.SeedCustomers(50));
        Assert.Null(exception);
    }

    [Fact]
    public async Task Test_ExecuteAsync_CreatesNRows()
    {
        // Arrange
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
        // Arrange
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
