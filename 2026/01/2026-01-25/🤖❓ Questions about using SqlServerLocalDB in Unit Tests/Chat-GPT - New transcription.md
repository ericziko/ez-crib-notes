---
uid: 42fe7f46-c082-4c9e-b283-8013919f4eef
---
***

# Chat-GPT - New transcription
You can absolutely do "real" integration tests with **[SQL Server](https://app.getrecall.ai/item/a02e5730-6e02-480d-978c-ddeee777e4c7) LocalDB**—you just have to be a bit deliberate about **database creation, script execution (GO batches), parallelism, and teardown**.

Below is a pragmatic setup that works well in **.NET 9 + xUnit**, no Docker required.

## The core idea

For each test (or test class / suite), you:

1. Connect to `master` on `(localdb)\\MSSQLLocalDB`
2. `CREATE DATABASE` with a unique name (often also unique MDF/LDF file paths in `%TEMP%`)
3. Run your [T-SQL](https://app.getrecall.ai/item/db51b572-da06-4d55-97dd-1c11d58e667c) scripts to build schema + seed data
4. Run tests
5. Tear down: force-close connections, `DROP DATABASE`, delete MDF/LDF

LocalDB is fast enough for this if you scope it wisely.

## Pick your isolation level (3 common patterns)

### 1) Database per test (maximum isolation, slower)

- Each test creates its own DB and drops it.
- Great when tests mutate data heavily and you want zero cross-test coupling.

### 2) Database per test class / collection (sweet spot)

- Use an xUnit `CollectionFixture` so all tests in a class/collection share one DB.
- Reset state **between tests** (optional) using either:
- re-run seed scripts, or
- delete/truncate tables, or
- a library like **Respawn** (works great with [SQL Server](https://app.getrecall.ai/item/a02e5730-6e02-480d-978c-ddeee777e4c7)/LocalDB).

### 3) Database per test run (fastest, least isolated)

- One DB for the whole test run, reset state between tests.

Given your goal ("each test, group, or suite"), I'd implement **#2** and optionally allow **#1** for the handful of tests that truly need it.

## The "gotchas" you must handle

### GO batch separators

If your `.sql` files contain `GO`, `SqlCommand` will choke because `GO` is not [T-SQL](https://app.getrecall.ai/item/db51b572-da06-4d55-97dd-1c11d58e667c); it's a tooling batch separator.

So you need to split scripts on lines that are exactly `GO` (case-insensitive), then execute each batch separately.

### Teardown can fail due to pooled connections

Even if you `Dispose()` your DbContext, a pooled connection can keep the DB "in use".

Fixes:

- Ensure all connections are disposed
- Call `SqlConnection.ClearAllPools()`
- Drop with:
- `ALTER DATABASE ... SET SINGLE_USER WITH ROLLBACK IMMEDIATE;`
- then `DROP DATABASE ...;`

### xUnit parallelization

If you do "DB per test", parallel is usually fine because DB names are unique. If you share a DB (per class/collection), either:

- disable parallelization for that collection, or
- use a lock + reset per test.

## A concrete implementation (xUnit fixture)

This example:

- creates a unique DB per fixture
- runs scripts from a folder in order
- drops the DB and deletes MDF/LDF

### 1) The fixture

```
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using Xunit;

public sealed class LocalDbDatabaseFixture : IAsyncLifetime
{
    private readonly string _instance = @"(localdb)\\MSSQLLocalDB";
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");
    private string? _mdfPath;
    private string? _ldfPath;

    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _mdfPath = Path.Combine(Path.GetTempPath(), $"{_dbName}.mdf");
        _ldfPath = Path.Combine(Path.GetTempPath(), $"{_dbName}.ldf");

        var masterCsn = new SqlConnectionStringBuilder
        {
            DataSource = _instance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
        }.ConnectionString;

        // Create DB with explicit files so we can delete them reliably.
        await using (var conn = new SqlConnection(masterCsn))
        {
            await conn.OpenAsync();

            var createDbSql = $@"
CREATE DATABASE [{_dbName}]
ON (NAME = N'{_dbName}', FILENAME = N'{_mdfPath}')
LOG ON (NAME = N'{_dbName}_log', FILENAME = N'{_ldfPath}');";

            await using var cmd = new SqlCommand(createDbSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = _instance,
            InitialCatalog = _dbName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
        }.ConnectionString;

        // Run schema + seed scripts
        await RunSqlScriptsAsync(ConnectionString, scriptsFolder: "TestSql");
    }

    public async Task DisposeAsync()
    {
        var masterCsn = new SqlConnectionStringBuilder
        {
            DataSource = _instance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
        }.ConnectionString;

        // Clear pools so DROP DATABASE doesn't fail due to pooled connections.
        SqlConnection.ClearAllPools();

        await using (var conn = new SqlConnection(masterCsn))
        {
            await conn.OpenAsync();

            var dropSql = $@"
IF DB_ID(N'{_dbName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_dbName}];
END";

            await using var cmd = new SqlCommand(dropSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        TryDelete(_mdfPath);
        TryDelete(_ldfPath);
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    private static async Task RunSqlScriptsAsync(string csn, string scriptsFolder)
    {
        // e.g. TestSql/001_schema.sql, 002_seed.sql, etc.
        var baseDir = AppContext.BaseDirectory;
        var folder = Path.Combine(baseDir, scriptsFolder);
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"SQL scripts folder not found: {folder}");

        var files = Directory.GetFiles(folder, "*.sql")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await using var conn = new SqlConnection(csn);
        await conn.OpenAsync();

        foreach (var file in files)
        {
            var sql = await File.ReadAllTextAsync(file, Encoding.UTF8);
            foreach (var batch in SplitOnGo(sql))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                await using var cmd = new SqlCommand(batch, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 60
                };
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    private static IEnumerable<string> SplitOnGo(string sql)
    {
        // Split on lines that contain only "GO" (ignoring whitespace) – SSMS-style.
        var sb = new StringBuilder();
        using var reader = new StringReader(sql);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return sb.ToString();
                sb.Clear();
            }
            else
            {
                sb.AppendLine(line);
            }
        }
        if (sb.Length > 0)
            yield return sb.ToString();
    }
}
```

### 2) Use it in tests (per test class)

```
public class MyRepositoryTests : IClassFixture<LocalDbDatabaseFixture>
{
    private readonly LocalDbDatabaseFixture _db;

    public MyRepositoryTests(LocalDbDatabaseFixture db) => _db = db;

    [Fact]
    public async Task Can_insert_and_query()
    {
        await using var conn = new SqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        // run your SUT using _db.ConnectionString (or configure DbContext with it)
    }
}
```

If you want *per suite / multiple classes* sharing one DB, wrap it with a **CollectionFixture**:

```
[CollectionDefinition("LocalDb")]
public class LocalDbCollection : ICollectionFixture<LocalDbDatabaseFixture> { }

[Collection("LocalDb")]
public class TestA { /* uses fixture */ }

[Collection("LocalDb")]
public class TestB { /* uses fixture */ }
```

## Where EF Core fits (even if you prefer scripts)

Even if you "build from scripts", EF can still help for wiring:

- Configure DbContext to point at the fixture's connection string.
- Optionally use EF migrations instead of scripts later.

But if your source of truth is [T-SQL](https://app.getrecall.ai/item/db51b572-da06-4d55-97dd-1c11d58e667c) scripts, keep it that way—tests should deploy the same way prod deploys.

## Optional: fast resets between tests (instead of recreating DB)

If you find "DB per test" too slow, do:

- one DB per class/collection
- reset between tests by re-running `seed.sql`, or truncating tables, or using Respawn.

That gets you 80% of the isolation with way less churn.

If you implement the fixture above, you're basically running a tiny, local, deterministic database factory—like Testcontainers, but with training wheels made of MDF files. The universe is strange; your tests don't have to be.

## [Chat-Gpt - First - Transcription](<Chat-Gpt - First - Transcription.md>)

