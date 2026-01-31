---
uid: 7515f212-2fb9-4d93-8bb3-21b21b244f72
title: SqlServer LocalDB in Unit tests
created: 2026-01-30T16:50:14
modified: 2026-01-30T17:22:18
---

# SqlServer LocalDB in Unit tests
Cribbed from: Chat-GPT conversation

## 🤖❓ Chat-GPT Question 

- I am working on a .NET 9.0 application
- I do not have access to docker
- I only have access to SQLServerLocalDB
- I would like to be able to write integration tests in `XUnit` and for each integration test, group of integration tests, or test suite be able to create new instances of my SqlServer database in SQLLocalDB
- So I will need to be able to create the database from scratch using SQL Server T-SQL scripts, run the unit tests and tear the database down. 
- Can you give me some guidance around how I might do this

## **The core idea**

For each test (or test class / suite), you:

1. Connect to master on (localdb)\MSSQLLocalDB
2. CREATE DATABASE with a unique name (often also unique MDF/LDF file paths in %TEMP%)
3. Run your T-SQL scripts to build schema + seed data
4. Run tests
5. Tear down: force-close connections, DROP DATABASE, delete MDF/LDF

LocalDB is fast enough for this if you scope it wisely.

## Pick your isolation level (3 common patterns)

### 1) Database per test (maximum isolation, slower)

- Each test creates its own DB and drops it.
- Great when tests mutate data heavily and you want zero cross-test coupling.

### 2) Database per test class / collection (sweet spot)

- Use an xUnit CollectionFixture so all tests in a class/collection share one DB.
- Reset state **between tests** (optional) using either:
    - re-run seed scripts, or
    - delete/truncate tables, or
    - a library like **Respawn** (works great with SQL Server/LocalDB).

### 3) Database per test run (fastest, least isolated)

- One DB for the whole test run, reset state between tests.

Given your goal ("each test, group, or suite"), I'd implement **#2** and optionally allow **#1** for the handful of tests that truly need it.

---

## The "gotchas" you must handle

### GO batch separators

If your .sql files contain GO, SqlCommand will choke because GO is not T-SQL; it's a tooling batch separator.
So you need to split scripts on lines that are exactly GO (case-insensitive), then execute each batch separately.

### Teardown can fail due to pooled connections

Even if you Dispose() your DbContext, a pooled connection can keep the DB "in use".

Fixes:

- Ensure all connections are disposed
- Call SqlConnection.ClearAllPools()
- Drop with:

```sql
    - ALTER DATABASE ... SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    - then DROP DATABASE ...;
```

### xUnit parallelization

If you do "DB per test", parallel is usually fine because DB names are unique.
If you share a DB (per class/collection), either:
- disable parallelization for that collection, or
- use a lock + reset per test.

## A concrete implementation (xUnit fixture)

This example:
- creates a unique DB per fixture
- runs scripts from a folder in order
- drops the DB and deletes MDF/LDF

### 1) The fixture

```cs
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using Xunit;

public sealed class LocalDbDatabaseFixture : IAsyncLifetime
{
    private readonly string _instance = @"(localdb)\MSSQLLocalDB";
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

## Where EF Core fits (even if you prefer scripts)

Even if you "build from scripts", EF can still help for wiring:
- Configure DbContext to point at the fixture's connection string.
- Optionally use EF migrations instead of scripts later.
But if your source of truth is T-SQL scripts, keep it that way—tests should deploy the same way prod deploys.

## Optional: fast resets between tests (instead of recreating DB)

If you find "DB per test" too slow, do:
- one DB per class/collection
- reset between tests by re-running seed.sql, or truncating tables, or using Respawn.

That gets you 80% of the isolation with way less churn.

If you implement the fixture above, you're basically running a tiny, local, deterministic database factory—like Testcontainers, but with training wheels made of MDF files. The universe is strange; your tests don't have to be.

## 🤖❓ What is `respwan`

- [GitHub - jbogard/Respawn: Intelligent database cleaner for integration tests](https://github.com/jbogard/Respawn?utm_source=chatgpt.com)

**Respawn** is a small **.NET test utility** that "resets" a database back to a clean baseline **by deleting data from tables intelligently**, instead of dropping/recreating the whole database or wrapping every test in a transaction.

- **Fast resets**: Great when you want *one database per test class/suite* but still need *each test to start clean*. 
	- [Faster .NET Database Integration Tests with Respawn and xUnit \| Khalid Abuhakmeh](https://khalidabuhakmeh.com/faster-dotnet-database-integration-tests-with-respawn-and-xunit?utm_source=chatgpt.com)
- **Keeps schema**: It typically wipes rows while leaving tables, views, etc. intact—so you run your migrations/scripts once, then Respawn cleans between tests. 
- **Configurable**: You can exclude tables/schemas (e.g., migration history tables) so it doesn't blow away things you need. 
  - [Clean database before executing a test in .NET - Stack Overflow](https://stackoverflow.com/questions/19213575/clean-database-before-executing-a-test-in-net)

Typical flow looks like:

1. Create DB + apply schema (scripts/migrations) once for the fixture
2. Initialize a Respawner pointing at that DB
3. Before each test: await respawner.ResetAsync(connection);
4. Seed only the data your test needs

That's the "database per suite, clean per test" pattern—usually the best balance of speed + isolation when you can't spin up Docker containers.

If you want, I can drop a minimal xUnit fixture snippet showing Respawn with LocalDB and how to exclude __EFMigrationsHistory (or your own "static reference data" tables).

### Here's a minimal, working xUnit + LocalDB + Respawn setup that gives you

- **DB created once per test collection** (fast)
- **schema applied once** (scripts or EF migrations)
- **database reset before each test** (clean slate)

### 1) Install Respawn

```sh
dotnet add package Respawn
dotnet add package Microsoft.Data.SqlClient
```

### 2) Collection fixture: create DB once + build a Respawner

```cs
using Microsoft.Data.SqlClient;
using Respawn;

public sealed class LocalDbRespawnFixture : IAsyncLifetime
{
    private readonly string _instance = @"(localdb)\MSSQLLocalDB";
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");

    public string ConnectionString { get; private set; } = default!;
    public Respawner Respawner { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var masterCsn = new SqlConnectionStringBuilder
        {
            DataSource = _instance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
        }.ConnectionString;

        // Create the database once
        await using (var conn = new SqlConnection(masterCsn))
        {
            await conn.OpenAsync();
            var sql = $@"
IF DB_ID(N'{_dbName}') IS NULL
BEGIN
    CREATE DATABASE [{_dbName}];
END";
            await using var cmd = new SqlCommand(sql, conn);
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

        // Apply schema once (choose ONE approach)

        // A) Run your .sql scripts (recommended if scripts are your source of truth)
        await ApplySqlScriptsAsync(ConnectionString, scriptsFolder: "TestSql");

        // B) Or apply EF migrations once (optional)
        // using var db = new YourDbContext(new DbContextOptionsBuilder<YourDbContext>()
        //     .UseSqlServer(ConnectionString).Options);
        // await db.Database.MigrateAsync();

        // Create Respawner once (it will wipe data between tests)
        await using var dbConn = new SqlConnection(ConnectionString);
        await dbConn.OpenAsync();

        Respawner = await Respawner.CreateAsync(dbConn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = new[] { "dbo" },

            // If you use EF migrations, keep this table:
            TablesToIgnore = new Respawn.Graph.Table[]
            {
                "__EFMigrationsHistory"
            },

            // Reset identity columns too (super handy)
            WithReseed = true
        });
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

        SqlConnection.ClearAllPools();

        await using var conn = new SqlConnection(masterCsn);
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

    private static async Task ApplySqlScriptsAsync(string csn, string scriptsFolder)
    {
        // Minimal stub: put your own script runner here (including GO-splitting).
        // If you already have the earlier GO-splitting helper, call it here.
        var baseDir = AppContext.BaseDirectory;
        var folder = Path.Combine(baseDir, scriptsFolder);
        var files = Directory.GetFiles(folder, "*.sql").OrderBy(x => x).ToArray();

        await using var conn = new SqlConnection(csn);
        await conn.OpenAsync();

        foreach (var file in files)
        {
            var sql = await File.ReadAllTextAsync(file);
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
```

> Note: The ApplySqlScriptsAsync stub above doesn't split GO. If your scripts include GO, use a GO-splitting runner (like the one I showed earlier) and call it here.

### 3) Make it an xUnit collection fixture (shared DB across classes)

```cs
using Xunit;

[CollectionDefinition("LocalDb")]
public class LocalDbCollection : ICollectionFixture<LocalDbRespawnFixture> { }
```

## 4) Reset before each test (xUnit creates a new test class instance per test)

Because xUnit creates a new instance of your test class **per test**, you can use IAsyncLifetime.InitializeAsync() to reset **before each test** without any weird hooks.

```cs
using Microsoft.Data.SqlClient;
using Xunit;

[Collection("LocalDb")]
public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly LocalDbRespawnFixture _db;

    public OrderRepositoryTests(LocalDbRespawnFixture db) => _db = db;

    public async Task InitializeAsync()
    {
        await using var conn = new SqlConnection(_db.ConnectionString);
        await conn.OpenAsync();
        await _db.Respawner.ResetAsync(conn);

        // Optional: seed per-test baseline here if you want
        // await SeedAsync(conn);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Can_insert_and_query()
    {
        // Arrange / Act / Assert using _db.ConnectionString
    }
}
```

### **A couple of nerdy-but-important notes**

- Respawn resets **tables**. If you have "static reference data" you want to keep (e.g., lookup tables), you can add those to TablesToIgnore.
- If you share the same DB across tests/classes, consider disabling parallelization for that collection if your tests aren't designed to run concurrently against one database.

That's it: **create once, reset often**—like washing a lab beaker instead of rebuilding the entire lab each experiment.
