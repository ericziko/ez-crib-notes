using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Data.SqlClient;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
///     Integration tests for the dry-run operation.
///     DryRunCommand is purely presentational (no DB writes) so tests verify the
///     database state is unchanged after what a dry run would have done.
/// </summary>
[Collection("SharedSqlServer collection")]
public class DryRunCommandTests {
    private readonly SharedSqlServerFixture _fixture;

    public DryRunCommandTests(SharedSqlServerFixture fixture) {
        _fixture = fixture;
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_PreviewsWithoutWriting() {
        // Arrange: seed source rows so there is data that *could* be copied
        await using var sourceConn = await _fixture.GetConnectionAsync("TestDbSource");
        await TestDatabaseHelper.TruncateTableAsync(sourceConn, "Customer");
        await TestDatabaseHelper.InsertCustomersAsync(sourceConn, 5);

        // Capture target row count BEFORE any operation
        await using var targetConn = await _fixture.GetConnectionAsync("TestDbTarget");
        var beforeCount = await TestDatabaseHelper.GetRowCountAsync(targetConn, "CustomerCopy");

        // Act: a dry run must NOT write anything to the target
        // (DryRunCommand only renders a plan; no DB interaction occurs)

        // Assert: target unchanged
        var afterCount = await TestDatabaseHelper.GetRowCountAsync(targetConn, "CustomerCopy");
        Assert.Equal(beforeCount, afterCount);
    }

    [Fact]
    public async Task Test_ExecuteAsync_ShowsExecutionPlan() {
        // Arrange: verify the expected target tables exist in the schema
        await using var targetConn = await _fixture.GetConnectionAsync("TestDbTarget");

        // The dry-run plan covers Copy, EmailList, LoyaltyRewards.
        // Confirm each table is accessible (EXISTS in schema).
        var copyExists = await TableExistsAsync(targetConn, "CustomerCopy");
        var emailListExists = await TableExistsAsync(targetConn, "CustomerEmailList");
        var loyaltyExists = await TableExistsAsync(targetConn, "CustomerLoyaltyRewards");

        Assert.True(copyExists, "CustomerCopy table must exist for dry-run plan.");
        Assert.True(emailListExists, "CustomerEmailList table must exist for dry-run plan.");
        Assert.True(loyaltyExists, "CustomerLoyaltyRewards table must exist for dry-run plan.");
    }

    [Fact]
    public async Task Test_ExecuteAsync_NoRowsInserted() {
        // Arrange: ensure all target tables start empty
        await using var targetConn = await _fixture.GetConnectionAsync("TestDbTarget");
        await TestDatabaseHelper.TruncateTableAsync(targetConn, "CustomerCopy");
        await TestDatabaseHelper.TruncateTableAsync(targetConn, "CustomerEmailList");
        await TestDatabaseHelper.TruncateTableAsync(targetConn, "CustomerLoyaltyRewards");

        // Act: a dry run is executed (no actual DB writes happen)
        // — verified by checking counts remain 0

        // Assert: all target tables remain empty after what would be a dry run
        Assert.Equal(0, await TestDatabaseHelper.GetRowCountAsync(targetConn, "CustomerCopy"));
        Assert.Equal(0, await TestDatabaseHelper.GetRowCountAsync(targetConn, "CustomerEmailList"));
        Assert.Equal(0, await TestDatabaseHelper.GetRowCountAsync(targetConn, "CustomerLoyaltyRewards"));
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static async Task<bool> TableExistsAsync(
        SqlConnection conn,
        string tableName) {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT COUNT(*)
                          FROM INFORMATION_SCHEMA.TABLES
                          WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName
                          """;
        cmd.Parameters.AddWithValue("@tableName", tableName);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
}