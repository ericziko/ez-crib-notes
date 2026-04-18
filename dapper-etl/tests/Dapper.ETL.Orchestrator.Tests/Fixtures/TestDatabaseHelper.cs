using Microsoft.Data.SqlClient;

namespace Dapper.ETL.Orchestrator.Tests.Fixtures;

/// <summary>
/// Static helpers for seeding and inspecting test databases.
/// </summary>
public static class TestDatabaseHelper
{
    /// <summary>
    /// Inserts <paramref name="count"/> synthetic Customer rows into
    /// TestDbSource.dbo.Customer.  The connection must already be open and
    /// targeting TestDbSource.
    /// </summary>
    public static async Task InsertCustomersAsync(SqlConnection conn, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO dbo.Customer (CustomerId, FirstName, LastName, EmailAddress)
                VALUES (@id, @first, @last, @email)
                """;
            cmd.Parameters.AddWithValue("@id", i);
            cmd.Parameters.AddWithValue("@first", $"FirstName{i}");
            cmd.Parameters.AddWithValue("@last", $"LastName{i}");
            cmd.Parameters.AddWithValue("@email", $"user{i}@test.example.com");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Returns the row count for <paramref name="table"/> in
    /// <paramref name="schema"/> (defaults to dbo).
    /// The connection must already be open and targeting the correct database.
    /// </summary>
    public static async Task<int> GetRowCountAsync(
        SqlConnection conn,
        string table,
        string schema = "dbo")
    {
        await using var cmd = conn.CreateCommand();
        // Table and schema are not user-supplied at runtime so inline is safe here.
        cmd.CommandText = $"SELECT COUNT(*) FROM [{schema}].[{table}]";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Truncates <paramref name="table"/> in <paramref name="schema"/>.
    /// The connection must already be open and targeting the correct database.
    /// </summary>
    public static async Task TruncateTableAsync(
        SqlConnection conn,
        string table,
        string schema = "dbo")
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"TRUNCATE TABLE [{schema}].[{table}]";
        await cmd.ExecuteNonQueryAsync();
    }
}
