using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Fixtures;

public class SqlServerFixture : IAsyncLifetime
{
    private const string Password = "TestPassword123!";

    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithPassword(Password)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var sqlScript = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "scripts", "init.sql"));

        await ExecuteInitScriptAsync(sqlScript);
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Returns an open SqlConnection to the specified database.</summary>
    /// <param name="database">One of: TestDbSource, TestDbTarget, EtlLogs, or empty string for master.</param>
    public async Task<SqlConnection> GetConnectionAsync(string database = "")
    {
        var conn = new SqlConnection(GetConnectionString(database));
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>Returns a connection string for the specified database.</summary>
    public string GetConnectionString(string database = "")
    {
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    private async Task ExecuteInitScriptAsync(string sqlScript)
    {
        // Split the script into batches on GO (on its own line, case-insensitive).
        // Each batch may contain a USE <db> statement; we detect that and switch
        // the connection string accordingly so the remainder of the batch runs
        // against the correct database.
        var batches = Regex.Split(sqlScript, @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        var currentDatabase = "";

        foreach (var raw in batches)
        {
            var batch = raw.Trim();
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            // Detect USE <database>; statement in this batch.
            var useMatch = Regex.Match(batch,
                @"^\s*USE\s+(\w+)\s*;?\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (useMatch.Success)
            {
                currentDatabase = useMatch.Groups[1].Value;
                // Nothing else to execute in a pure USE batch.
                if (Regex.IsMatch(batch, @"^\s*USE\s+\w+\s*;?\s*$",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    continue;
            }

            await using var conn = new SqlConnection(GetConnectionString(currentDatabase));
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = batch;
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly location until we find the scripts/ directory.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "scripts")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root containing scripts/init.sql.");
    }
}
