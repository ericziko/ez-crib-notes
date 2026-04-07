---
uid: c53a414d-b41e-445c-b15a-d2307ee45434
title: SqlLocalDB File Location
created: 2026-01-30T17:00:37
modified: 2026-01-30T17:20:26
tags:
  - 1
---

# SqlLocalDB File Location

## 🤖❓ when calling creat db on sqllocaldb - can I specify the files location where the database files get created?

Yes—you absolutely can. SQL Server LocalDB looks friendly and automatic, but under the hood it's still SQL Server, complete with very real .mdf and .ldf files that must live somewhere on disk. You just have to be explicit.

### **The short version**

When you call CREATE DATABASE, you can specify the exact file paths using ON (NAME = ..., FILENAME = ...).

LocalDB will happily comply—as long as:

- the directory exists
- your user account has write permissions
- the path is local (no UNC shares)

### **Canonical example**

```sql
CREATE DATABASE MyTestDb
ON PRIMARY
(
    NAME = MyTestDb_Data,
    FILENAME = 'C:\SqlLocalDb\Data\MyTestDb.mdf',
    SIZE = 50MB,
    FILEGROWTH = 10MB
)
LOG ON
(
    NAME = MyTestDb_Log,
    FILENAME = 'C:\SqlLocalDb\Data\MyTestDb.ldf',
    SIZE = 20MB,
    FILEGROWTH = 10MB
);
```

That is 100% valid for LocalDB.

### **What happens if you don't specify paths**

LocalDB defaults to something like:

```code
%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\<InstanceName>\
```

Which is fine until you want:

- deterministic test environments
- easy teardown
- parallel test runs
- artifact capture in CI logs
- or just knowing where your bits live

At that point, explicit paths stop being optional and start being sanity-preserving.

### **PowerShell + LocalDB pattern (integration-test friendly)**

A common trick is to generate a unique folder per test run:

```powershell
$runId = [guid]::NewGuid().ToString()
$basePath = "C:\SqlLocalDb\TestRuns\$runId"
New-Item -ItemType Directory -Path $basePath | Out-Null

$sql = @"
CREATE DATABASE TestDb_$runId
ON PRIMARY
(
    NAME = TestDb_Data,
    FILENAME = '$basePath\TestDb.mdf'
)
LOG ON
(
    NAME = TestDb_Log,
    FILENAME = '$basePath\TestDb.ldf'
);
"@

sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $sql
```

Now each test run is hermetic, disposable, and inspectable. Science.

### **Gotchas (LocalDB has opinions)**

- **Directories must exist** – SQL Server will not create them
- **No network paths** – LocalDB is strictly local
- **File locks persist** – detach or drop cleanly before deletion
- **Parallel runs** – unique DB names *and* unique file paths

### Pro-tip 

#### DROP DATABASE is not enough

LocalDB can keep file handles around briefly. For test teardown, it's safer to:

```sql
ALTER DATABASE MyDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE MyDb;
```

Then delete the directory.

#### Why this matters philosophically

Tests that "just work" because SQL Server hides things are fragile. Tests that control their storage are *honest*. Honest tests fail loudly, clean up after themselves, and don't summon ghosts from %LOCALAPPDATA%.

If you want, the next natural step is wiring this into xUnit fixtures so each test class—or even each test—gets its own LocalDB instance with deterministic paths and teardown.

# 🤖❓ Show me with Xunit

Here's a clean, copy-pasteable xUnit pattern that:

- creates a **unique LocalDB database per test class**
- **pins the MDF/LDF paths** to a folder you control
- applies a schema script (optional)
- drops the DB on teardown (with "no, really, drop it" semantics)

## 1) Test fixture: create DB with explicit file locations

```csharp
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public sealed class LocalDbDatabaseFixture : IAsyncLifetime
{
    // Change to your LocalDB instance name if needed:
    private const string LocalDbInstance = @"(localdb)\MSSQLLocalDB";

    public string DatabaseName { get; } = $"TestDb_{Guid.NewGuid():N}";
    public string BaseFolder { get; }
    public string ConnectionString { get; private set; } = default!;

    public LocalDbDatabaseFixture()
    {
        // Put it wherever you want: temp, repo .artifacts, etc.
        BaseFolder = Path.Combine(Path.GetTempPath(), "sqllocaldb-tests", DatabaseName);
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(BaseFolder);

        var mdfPath = Path.Combine(BaseFolder, $"{DatabaseName}.mdf");
        var ldfPath = Path.Combine(BaseFolder, $"{DatabaseName}.ldf");

        // Connect to master to create the new DB
        var masterCs = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;

        await using (var conn = new SqlConnection(masterCs))
        {
            await conn.OpenAsync();

            var createSql = $@"
CREATE DATABASE [{DatabaseName}]
ON PRIMARY (
    NAME = N'{DatabaseName}_Data',
    FILENAME = N'{mdfPath}'
)
LOG ON (
    NAME = N'{DatabaseName}_Log',
    FILENAME = N'{ldfPath}'
);";

            await using (var cmd = new SqlCommand(createSql, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;

        // Optional: apply schema
        await ApplySchemaAsync();
    }

    public async Task DisposeAsync()
    {
        // Drop from master (and force-close connections)
        var masterCs = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;

        await using (var conn = new SqlConnection(masterCs))
        {
            await conn.OpenAsync();

            var dropSql = $@"
IF DB_ID(N'{DatabaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{DatabaseName}];
END";

            await using var cmd = new SqlCommand(dropSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // Best-effort cleanup of files
        TryDeleteDirectory(BaseFolder);
    }

    private async Task ApplySchemaAsync()
    {
        // Replace with "read .sql file and execute" if you want.
        const string schemaSql = @"
CREATE TABLE dbo.Widget (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL
);";

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(schemaSql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // LocalDB can hang onto file locks briefly.
            // In real life you might retry with a short backoff.
        }
    }
}
```

### 2) Wire it to a test class with `IClassFixture<>`

```cs
using System.Data.SqlClient;
using System.Threading.Tasks;
using Xunit;

public class WidgetTests : IClassFixture<LocalDbDatabaseFixture>
{
    private readonly LocalDbDatabaseFixture _db;

    public WidgetTests(LocalDbDatabaseFixture db)
        => _db = db;

    [Fact]
    public async Task Insert_and_read_back()
    {
        await using var conn = new SqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        // Insert
        await using (var cmd = new SqlCommand(
            "INSERT INTO dbo.Widget(Name) VALUES (@name); SELECT SCOPE_IDENTITY();", conn))
        {
            cmd.Parameters.AddWithValue("@name", "Gizmo");
            var idObj = await cmd.ExecuteScalarAsync();
            Assert.NotNull(idObj);
        }

        // Read
        await using (var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Widget WHERE Name = @name;", conn))
        {
            cmd.Parameters.AddWithValue("@name", "Gizmo");
            var count = (int)await cmd.ExecuteScalarAsync();
            Assert.Equal(1, count);
        }
    }
}
```

### **Notes (because LocalDB is polite but stubborn)**

- If you run tests **in parallel**, this pattern is safe because each class gets a unique DB name *and* unique file paths.
- If you want **one DB per test method**, you can switch from IClassFixture<> to building a small helper that each test calls (or use IAsyncLifetime on the test class itself and generate a new DB per test instance).

If you tell me whether you're using **EF Core** in the tests (or straight ADO.NET like above), I can tailor the fixture to either:

- run migrations (dbContext.Database.Migrate()), or
- run your .sql files in order (with GO splitting), which is very common for "recreate from scripts" workflows.

## 🤖❓ how can i attach an existing mdf file

Attaching an existing .mdf to SQL Server LocalDB is one of those things that feels mystical until you remember: LocalDB **is** SQL Server, just wearing sweatpants.

There are **three sane ways** to do it, depending on how much control and ceremony you want.

### 1. Attach with CREATE DATABASE … FOR ATTACH 
(the explicit, honest way)

This is the clearest and most deterministic approach—perfect for tests and tooling.

```sql
CREATE DATABASE MyAttachedDb
ON 
(
    FILENAME = 'C:\SqlLocalDb\Data\MyDatabase.mdf'
),
(
    FILENAME = 'C:\SqlLocalDb\Data\MyDatabase_log.ldf'
)
FOR ATTACH;
```

### 💎 If you don't know the log file location (or it's missing)

```sql
CREATE DATABASE MyAttachedDb
ON 
(
    FILENAME = 'C:\SqlLocalDb\Data\MyDatabase.mdf'
)
FOR ATTACH_REBUILD_LOG;
```

### 2. Attach by just opening the .mdf (connection-string magic)

SQL Server can auto-attach a database when you connect to it.

```cs
var cs = new SqlConnectionStringBuilder
{
    DataSource = @"(localdb)\MSSQLLocalDB",
    AttachDBFilename = @"C:\SqlLocalDb\Data\MyDatabase.mdf",
    IntegratedSecurity = true,
    TrustServerCertificate = true
}.ConnectionString;
```

Behind the scenes, SQL Server does a quiet attach.

This is:

- ✔️ great for quick tests
- ❌ terrible if you care about DB names, lifecycle, or cleanup
- ❌ easy to accidentally attach the same file multiple times under different logical names

Think of this as *implicit magic*. Useful, but not virtuous.

### 3. Attach via sp_attach_db (the deprecated ancestor)

```sql
EXEC sp_attach_db 
    @dbname = N'MyAttachedDb',
    @filename1 = N'C:\SqlLocalDb\Data\MyDatabase.mdf',
    @filename2 = N'C:\SqlLocalDb\Data\MyDatabase_log.ldf';
```

This still works, but it's deprecated. Treat it like a rotary phone: charming, not future-facing.

### LocalDB-specific realities (important)

- **Paths must be local**
- **Directories must already exist**
- **Your user must own the files**
- **The** **.mdf** **cannot be attached to another SQL instance at the same time**
- Version compatibility matters:
    - You can attach **older** DBs to newer LocalDB
    - You cannot attach a DB from a *newer* SQL version

### xUnit-friendly attach pattern (fixture-style)
If you want to attach an existing MDF inside a test fixture:

```cs
var attachSql = @"
IF DB_ID(N'MyAttachedDb') IS NULL
BEGIN
    CREATE DATABASE MyAttachedDb
    ON (FILENAME = N'C:\SqlLocalDb\Data\MyDatabase.mdf')
    FOR ATTACH_REBUILD_LOG;
END";

using var conn = new SqlConnection(
    @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;");
conn.Open();

using var cmd = new SqlCommand(attachSql, conn);
cmd.ExecuteNonQuery();
```

Pair this with a teardown that:

1. sets SINGLE_USER
2. drops the DB
3. deletes the files (if they're test artifacts

---

### Philosophy break (brief, I promise)

Attaching MDFs is a **stateful act**. State is the enemy of reproducible tests unless you dominate it completely.

For most test suites:

- **Create-from-scripts** → best
- **Attach known MDF snapshot** → acceptable
- **Implicit auto-attach** → chaos gremlin

You're doing the right thing by asking *how*, but the deeper win is choosing *when*.

If you want, the next step is showing:

- attaching **read-only** snapshots
- cloning MDFs per test run
- or turning an MDF into a deterministic test seed without sharing state:

## 🤖❓ Show me all three

Sure—here are **all three** ways, shown end-to-end, with LocalDB in mind.

## 1) Explicit attach: CREATE DATABASE … FOR ATTACH (recommended)

### T-SQL (attach MDF + LDF)

```sql
-- Run against master
CREATE DATABASE [MyAttachedDb]
ON 
( FILENAME = N'C:\SqlLocalDb\Data\MyDatabase.mdf' ),
( FILENAME = N'C:\SqlLocalDb\Data\MyDatabase_log.ldf' )
FOR ATTACH;
```

### T-SQL (MDF only / rebuild log if needed)

```sql
-- Use when you do NOT have the log file, or it's unusable
CREATE DATABASE [MyAttachedDb]
ON ( FILENAME = N'C:\SqlLocalDb\Data\MyDatabase.mdf' )
FOR ATTACH_REBUILD_LOG;
```

### C# (execute it)

```cs
using var conn = new SqlConnection(
    @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true;");
conn.Open();

var sql = @"
CREATE DATABASE [MyAttachedDb]
ON (FILENAME = N'C:\SqlLocalDb\Data\MyDatabase.mdf'),
   (FILENAME = N'C:\SqlLocalDb\Data\MyDatabase_log.ldf')
FOR ATTACH;";

using var cmd = new SqlCommand(sql, conn);
cmd.ExecuteNonQuery();
```

## 2) Implicit attach: connection string  AttachDBFilename (auto-attach-on-connect)

This attaches when you connect (if it's not already attached).

### C# connection string

```cs
var cs = new SqlConnectionStringBuilder
{
    DataSource = @"(localdb)\MSSQLLocalDB",
    AttachDBFilename = @"C:\SqlLocalDb\Data\MyDatabase.mdf",
    IntegratedSecurity = true,
    TrustServerCertificate = true
}.ConnectionString;

using var conn = new SqlConnection(cs);
conn.Open();
```

### Notes (the "magic tax")

- SQL Server assigns a **logical database name** internally (often not what you expect).
- Cleanup can be annoying because you may not know what name it attached under.
- Great for quick dev, less great for deterministic test automation.

If you need the attached DB's name after connecting, you can query:

```sql
SELECT DB_NAME() AS CurrentDatabase;
```

### Bonus: deterministic teardown (works for any of the three)

Run against master:

```sql
IF DB_ID(N'MyAttachedDb') IS NOT NULL
BEGIN
    ALTER DATABASE [MyAttachedDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [MyAttachedDb];
END
```

If you used **AttachDBFilename**, you may not know the DB name. In that case, you can find it by file path:

```sql
SELECT d.name
FROM sys.databases d
JOIN sys.master_files mf ON d.database_id = mf.database_id
WHERE mf.physical_name = N'C:\SqlLocalDb\Data\MyDatabase.mdf';
```

Then drop that name.

If your goal is xUnit integration tests with "attach a seed MDF, run tests, drop DB, delete files," the cleanest approach is: **copy the MDF/LDF to a per-test folder, then do method #1 attach with explicit DB name**. That avoids shared state and parallel-run collisions.

## 🤖❓ show me copy the MDF/LDF to a per-test folder 
Then do method `#1` attach with explicit DB name. That avoids shared state and parallel-run collisions.

Here's the xUnit-ready pattern you want: **copy a seed MDF/LDF into a unique folder per test run**, then **attach via CREATE DATABASE … FOR ATTACH** with an explicit DB name.

This is the "parallel-safe, no shared state" way.

---

### Folder layout you'll have

#### Seed files (read-only, never mutated)

```
    ./TestData/SeedDb/SeedDb.mdf
    ./TestData/SeedDb/SeedDb_log.ldf
```

#### Per-test copy (unique)

```
    %TEMP%\sqllocaldb-tests\<DbName>\SeedDb.mdf
    %TEMP%\sqllocaldb-tests\<DbName>\SeedDb_log.ldf
```

#### 1) The fixture

```cs
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public sealed class LocalDbAttachedCopyFixture : IAsyncLifetime
{
    private const string LocalDbInstance = @"(localdb)\MSSQLLocalDB";

    public string DatabaseName { get; } = $"TestDb_{Guid.NewGuid():N}";
    public string WorkingFolder { get; }
    public string WorkingMdfPath { get; }
    public string WorkingLdfPath { get; }
    public string ConnectionString { get; private set; } = default!;

    // Point these to your seed files (committed to repo or generated once)
    private readonly string _seedMdfPath;
    private readonly string _seedLdfPath;

    public LocalDbAttachedCopyFixture()
    {
        // Seed DB files in your repo (example)
        var repoRoot = AppContext.BaseDirectory; // often bin/Debug/...; adjust if you prefer
        _seedMdfPath = Path.GetFullPath(Path.Combine(repoRoot, "..", "..", "..", "TestData", "SeedDb", "SeedDb.mdf"));
        _seedLdfPath = Path.GetFullPath(Path.Combine(repoRoot, "..", "..", "..", "TestData", "SeedDb", "SeedDb_log.ldf"));

        WorkingFolder = Path.Combine(Path.GetTempPath(), "sqllocaldb-tests", DatabaseName);
        WorkingMdfPath = Path.Combine(WorkingFolder, "SeedDb.mdf");
        WorkingLdfPath = Path.Combine(WorkingFolder, "SeedDb_log.ldf");
    }

    public async Task InitializeAsync()
    {
        // 1) Create isolated folder
        Directory.CreateDirectory(WorkingFolder);

        // 2) Copy seed MDF/LDF into it (so tests mutate the copies, not the seed)
        File.Copy(_seedMdfPath, WorkingMdfPath, overwrite: true);
        File.Copy(_seedLdfPath, WorkingLdfPath, overwrite: true);

        // Optional but helpful: ensure copies are NOT read-only (git can do this sometimes)
        EnsureWritable(WorkingMdfPath);
        EnsureWritable(WorkingLdfPath);

        // 3) Attach using method #1: CREATE DATABASE ... FOR ATTACH (explicit name)
        var masterCs = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;

        await using (var conn = new SqlConnection(masterCs))
        {
            await conn.OpenAsync();

            var attachSql = $@"
IF DB_ID(N'{DatabaseName}') IS NULL
BEGIN
    CREATE DATABASE [{DatabaseName}]
    ON (FILENAME = N'{WorkingMdfPath}'),
       (FILENAME = N'{WorkingLdfPath}')
    FOR ATTACH;
END";

            await using var cmd = new SqlCommand(attachSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        // 4) Drop the DB (force close)
        var masterCs = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbInstance,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;

        await using (var conn = new SqlConnection(masterCs))
        {
            await conn.OpenAsync();

            var dropSql = $@"
IF DB_ID(N'{DatabaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{DatabaseName}];
END";

            await using var cmd = new SqlCommand(dropSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // 5) Delete the copied files (best effort; LocalDB can hold locks briefly)
        TryDeleteDirectory(WorkingFolder);
    }

    private static void EnsureWritable(string path)
    {
        var attrs = File.GetAttributes(path);
        if ((attrs & FileAttributes.ReadOnly) != 0)
            File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // For rock-solid teardown, add a short retry loop here.
        }
    }
}
```

#### 2) Use it in a test class

```cs
using System.Data.SqlClient;
using System.Threading.Tasks;
using Xunit;

public class SeededDbTests : IClassFixture<LocalDbAttachedCopyFixture>
{
    private readonly LocalDbAttachedCopyFixture _db;

    public SeededDbTests(LocalDbAttachedCopyFixture db) => _db = db;

    [Fact]
    public async Task Can_read_seed_data()
    {
        await using var conn = new SqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        // Example: your seed DB already has dbo.Widget rows
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Widget;", conn);
        var count = (int)await cmd.ExecuteScalarAsync();

        Assert.True(count > 0);
    }

    [Fact]
    public async Task Can_mutate_without_affecting_other_tests()
    {
        await using var conn = new SqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        await using (var insert = new SqlCommand(
            "INSERT INTO dbo.Widget(Name) VALUES (@name);", conn))
        {
            insert.Parameters.AddWithValue("@name", "Per-test copy FTW");
            await insert.ExecuteNonQueryAsync();
        }

        await using (var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Widget WHERE Name = @name;", conn))
        {
            cmd.Parameters.AddWithValue("@name", "Per-test copy FTW");
            var count = (int)await cmd.ExecuteScalarAsync();
            Assert.Equal(1, count);
        }
    }
}
```

#### Parallel safety knobs (important)

xUnit runs test **classes** in parallel by default. This approach is safe because:

- unique DB name per fixture (Guid)
- unique folder paths per fixture
- no shared files

If you switch to *one DB per test method*, don't use IClassFixture<>. Use IAsyncLifetime on the test class itself (xUnit creates a new instance per test) or a helper factory.

#### Where do I get the "seed MDF/LDF" in the first place?

Two common ways:

1. **Build it once manually** (SSMS/SqlPackage), commit to repo under TestData/SeedDb/.
2. **Generate it in a build step** (run scripts/migrations, then detach and copy out MDF/LDF). More effort, more deterministic.

If you tell me whether your schema is created by **EF Core migrations** or **raw .sql scripts**, I can show the cleanest "build seed DB once" pipeline too.
