using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="ResetTargetDatabaseCommand"/> via <see cref="DataService.ResetTargetDatabase"/>.
/// </summary>
public class ResetTargetDatabaseCommandTests : IAsyncLifetime
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
    public async Task Test_ExecuteAsync_TruncatesAllTables()
    {
        // Arrange: populate all three target tables
        await SeedTargetTablesAsync(rowCount: 5);

        // Verify rows exist before reset
        await using var connBefore = await _fixture.GetConnectionAsync("TestDbTarget");
        var copyBefore  = await TestDatabaseHelper.GetRowCountAsync(connBefore, "CustomerCopy");
        var emailBefore = await TestDatabaseHelper.GetRowCountAsync(connBefore, "CustomerEmailList");
        Assert.True(copyBefore > 0,  "CustomerCopy should have rows before reset");
        Assert.True(emailBefore > 0, "CustomerEmailList should have rows before reset");

        // Act
        var dataService = new DataService(_configuration);
        await dataService.ResetTargetDatabase();

        // Assert: all target tables are empty
        await using var connAfter = await _fixture.GetConnectionAsync("TestDbTarget");
        var copyAfter    = await TestDatabaseHelper.GetRowCountAsync(connAfter, "CustomerCopy");
        var emailAfter   = await TestDatabaseHelper.GetRowCountAsync(connAfter, "CustomerEmailList");
        var loyaltyAfter = await TestDatabaseHelper.GetRowCountAsync(connAfter, "CustomerLoyaltyRewards");

        Assert.Equal(0, copyAfter);
        Assert.Equal(0, emailAfter);
        Assert.Equal(0, loyaltyAfter);
    }

    [Fact]
    public async Task Test_ExecuteAsync_ResetsSequences()
    {
        // Arrange: populate and then reset
        await SeedTargetTablesAsync(rowCount: 3);

        var dataService = new DataService(_configuration);
        await dataService.ResetTargetDatabase();

        // Act: insert one new row after reset — its identity-like ID should be 1
        await using var conn = await _fixture.GetConnectionAsync("TestDbTarget");
        await using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO dbo.CustomerEmailList (CustomerId, FirstName, LastName, EmailAddress)
            VALUES (1, 'Test', 'User', 'test@example.com');
            SELECT SCOPE_IDENTITY();
            """;
        var newId = await insertCmd.ExecuteScalarAsync();

        // Assert: after DBCC CHECKIDENT RESEED 0, the next identity value is 1
        Assert.Equal(1, Convert.ToInt32(newId));
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private async Task SeedTargetTablesAsync(int rowCount)
    {
        await using var conn = await _fixture.GetConnectionAsync("TestDbTarget");

        for (var i = 1; i <= rowCount; i++)
        {
            // CustomerCopy (no identity — uses explicit PK)
            await using var copyCmd = conn.CreateCommand();
            copyCmd.CommandText = """
                INSERT INTO dbo.CustomerCopy (CustomerId, FirstName, LastName, EmailAddress)
                VALUES (@id, @first, @last, @email)
                """;
            copyCmd.Parameters.AddWithValue("@id",    i);
            copyCmd.Parameters.AddWithValue("@first", $"First{i}");
            copyCmd.Parameters.AddWithValue("@last",  $"Last{i}");
            copyCmd.Parameters.AddWithValue("@email", $"user{i}@test.example.com");
            await copyCmd.ExecuteNonQueryAsync();

            // CustomerEmailList (uses sequence for PK default)
            await using var emailCmd = conn.CreateCommand();
            emailCmd.CommandText = """
                INSERT INTO dbo.CustomerEmailList (CustomerId, FirstName, LastName, EmailAddress)
                VALUES (@cid, @first, @last, @email)
                """;
            emailCmd.Parameters.AddWithValue("@cid",   i);
            emailCmd.Parameters.AddWithValue("@first", $"First{i}");
            emailCmd.Parameters.AddWithValue("@last",  $"Last{i}");
            emailCmd.Parameters.AddWithValue("@email", $"user{i}@test.example.com");
            await emailCmd.ExecuteNonQueryAsync();

            // CustomerLoyaltyRewards (uses sequence for PK default)
            await using var loyaltyCmd = conn.CreateCommand();
            loyaltyCmd.CommandText = """
                INSERT INTO dbo.CustomerLoyaltyRewards (CustomerId, LoyaltyRewordId, FirstName, LastName)
                VALUES (@cid, @lid, @first, @last)
                """;
            loyaltyCmd.Parameters.AddWithValue("@cid",   i);
            loyaltyCmd.Parameters.AddWithValue("@lid",   i);
            loyaltyCmd.Parameters.AddWithValue("@first", $"First{i}");
            loyaltyCmd.Parameters.AddWithValue("@last",  $"Last{i}");
            await loyaltyCmd.ExecuteNonQueryAsync();
        }
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
