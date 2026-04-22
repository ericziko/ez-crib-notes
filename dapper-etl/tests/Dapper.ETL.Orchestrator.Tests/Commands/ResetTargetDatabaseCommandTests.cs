using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for <see cref="ResetTargetDatabaseCommand" /> via <see cref="DataService.ResetTargetDatabase" />.
/// </summary>
[Collection("SharedSqlServer collection")]
public class ResetTargetDatabaseCommandTests {
    private readonly SharedSqlServerFixture _fixture;

    public ResetTargetDatabaseCommandTests(SharedSqlServerFixture fixture) {
        _fixture = fixture;
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_TruncatesAllTables() {
        // Arrange: populate all three target tables
        await SeedTargetTablesAsync(5);

        // Verify rows exist before reset
        await using var connBefore = await _fixture.GetConnectionAsync("TestDbTarget");
        var copyBefore = await TestDatabaseHelper.GetRowCountAsync(connBefore, "CustomerCopy");
        var emailBefore = await TestDatabaseHelper.GetRowCountAsync(connBefore, "CustomerEmailList");
        Assert.True(copyBefore > 0, "CustomerCopy should have rows before reset");
        Assert.True(emailBefore > 0, "CustomerEmailList should have rows before reset");

        // Act
        var dataService = BuildDataService();
        await dataService.ResetTargetDatabase();

        // Assert: all target tables are empty
        await using var connAfter = await _fixture.GetConnectionAsync("TestDbTarget");
        var copyAfter = await TestDatabaseHelper.GetRowCountAsync(connAfter, "CustomerCopy");
        var emailAfter = await TestDatabaseHelper.GetRowCountAsync(connAfter, "CustomerEmailList");
        var loyaltyAfter = await TestDatabaseHelper.GetRowCountAsync(connAfter, "CustomerLoyaltyRewards");

        Assert.Equal(0, copyAfter);
        Assert.Equal(0, emailAfter);
        Assert.Equal(0, loyaltyAfter);
    }

    [Fact]
    public async Task Test_ExecuteAsync_ResetsSequences() {
        // Arrange: populate and then reset
        await SeedTargetTablesAsync(3);

        var dataService = BuildDataService();
        await dataService.ResetTargetDatabase();

        // Act: insert one new row after reset — its identity-like ID should be 1
        await using var conn = await _fixture.GetConnectionAsync("TestDbTarget");
        await using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = """
                                INSERT INTO dbo.CustomerEmailList (CustomerId, FirstName, LastName, EmailAddress)
                                OUTPUT inserted.CustomerEmailId
                                VALUES (1, 'Test', 'User', 'test@example.com');
                                """;
        var newId = await insertCmd.ExecuteScalarAsync();

        // Assert: after DBCC CHECKIDENT RESEED 0, the next identity value is 1
        Assert.Equal(1, Convert.ToInt32(newId));
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private DataService BuildDataService() {
        return new DataService(
            _fixture.GetConnectionString("TestDbSource"),
            _fixture.GetConnectionString("TestDbTarget"),
            _fixture.GetConnectionString("EtlLogs"));
    }

    private async Task SeedTargetTablesAsync(int rowCount) {
        await using var conn = await _fixture.GetConnectionAsync("TestDbTarget");

        // Clear tables first to avoid PK conflicts from shared fixture
        await TestDatabaseHelper.TruncateTableAsync(conn, "CustomerCopy");
        await TestDatabaseHelper.TruncateTableAsync(conn, "CustomerEmailList");
        await TestDatabaseHelper.TruncateTableAsync(conn, "CustomerLoyaltyRewards");

        for (var i = 1; i <= rowCount; i++) {
            // CustomerCopy (no identity — uses explicit PK)
            await using var copyCmd = conn.CreateCommand();
            copyCmd.CommandText = """
                                  INSERT INTO dbo.CustomerCopy (CustomerId, FirstName, LastName, EmailAddress)
                                  VALUES (@id, @first, @last, @email)
                                  """;
            copyCmd.Parameters.AddWithValue("@id", i);
            copyCmd.Parameters.AddWithValue("@first", $"First{i}");
            copyCmd.Parameters.AddWithValue("@last", $"Last{i}");
            copyCmd.Parameters.AddWithValue("@email", $"user{i}@test.example.com");
            await copyCmd.ExecuteNonQueryAsync();

            // CustomerEmailList (uses sequence for PK default)
            await using var emailCmd = conn.CreateCommand();
            emailCmd.CommandText = """
                                   INSERT INTO dbo.CustomerEmailList (CustomerId, FirstName, LastName, EmailAddress)
                                   VALUES (@cid, @first, @last, @email)
                                   """;
            emailCmd.Parameters.AddWithValue("@cid", i);
            emailCmd.Parameters.AddWithValue("@first", $"First{i}");
            emailCmd.Parameters.AddWithValue("@last", $"Last{i}");
            emailCmd.Parameters.AddWithValue("@email", $"user{i}@test.example.com");
            await emailCmd.ExecuteNonQueryAsync();

            // CustomerLoyaltyRewards (uses sequence for PK default)
            await using var loyaltyCmd = conn.CreateCommand();
            loyaltyCmd.CommandText = """
                                     INSERT INTO dbo.CustomerLoyaltyRewards (CustomerId, LoyaltyRewordId, FirstName, LastName)
                                     VALUES (@cid, @lid, @first, @last)
                                     """;
            loyaltyCmd.Parameters.AddWithValue("@cid", i);
            loyaltyCmd.Parameters.AddWithValue("@lid", i);
            loyaltyCmd.Parameters.AddWithValue("@first", $"First{i}");
            loyaltyCmd.Parameters.AddWithValue("@last", $"Last{i}");
            await loyaltyCmd.ExecuteNonQueryAsync();
        }
    }
}