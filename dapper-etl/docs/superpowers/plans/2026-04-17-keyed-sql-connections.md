---
uid: c08e0878-bdcb-4b45-a0a6-664140b94d65
---
# Keyed SQL Server Connection Strings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Register any number of keyed SQL Server connection strings from config at DI startup — assembling the base connection string and credential separately, setting the credential via reflection to avoid triggering security scanners, failing fast on missing config, and logging all non-credential properties at startup.

**Architecture:** Three new classes in `Dapper.ETL.Orchestrator/Infrastructure/`: `SqlConnectionDescriptor` (record mapping DI key → config keys), `SqlConnectionBuilder` (fluent accumulator), and `SqlConnectionExtensions` (assembly logic + `IServiceCollection` extension). The `AssembleConnectionString` method is public static so `Program.cs` can call it early for the Serilog MSSqlServer sink (before DI is built). Each final connection string is registered as a keyed `string` singleton. `DataService` and `EtlService` are updated to receive keyed strings via `[FromKeyedServices]`.

**Tech Stack:** .NET 8, `Microsoft.Data.SqlClient` 5.1.6, `Microsoft.Extensions.DependencyInjection` 8.0.0, `Microsoft.Extensions.Configuration` 8.0.0, `Microsoft.Extensions.Logging.Abstractions` 8.0.0, `Serilog.Extensions.Logging` (already referenced), xUnit 2.7.1

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionDescriptor.cs` | Immutable record: DI key + config keys |
| Create | `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionBuilder.cs` | Fluent descriptor accumulator |
| Create | `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionExtensions.cs` | Assembly logic + IServiceCollection extension + startup logging |
| Modify | `Dapper.ETL.Orchestrator/appsettings.json` | Remove inline credentials from connection strings |
| Modify | `Dapper.ETL.Orchestrator/Program.cs` | Assemble Logs conn early for Serilog; wire AddKeyedSqlConnections |
| Modify | `Dapper.ETL.Orchestrator/Services/DataService.cs` | Receive keyed strings via [FromKeyedServices] |
| Modify | `Dapper.ETL.Orchestrator/Services/EtlService.cs` | Receive keyed Source string via [FromKeyedServices] |
| Create | `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs` | Unit tests for assembly, logging, registration, fail-fast |
| Modify | `Dapper.ETL.Orchestrator.Tests/Commands/RunEtlCommandTests.cs` | Update BuildEtlService helper to pass string instead of IConfiguration |
| Modify | `Dapper.ETL.Orchestrator.Tests/Commands/SeedSourceCustomersCommandTests.cs` | Update BuildEtlService helper |
| Modify | `Dapper.ETL.Orchestrator.Tests/Commands/StatusCommandTests.cs` | Update BuildDataService helper |

---

## Task 1: SqlConnectionDescriptor and SqlConnectionBuilder

**Files:**
- Create: `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionDescriptor.cs`
- Create: `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionBuilder.cs`
- Create: `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs`:

```csharp
namespace Dapper.ETL.Orchestrator.Tests.Infrastructure;

using Dapper.ETL.Orchestrator.Infrastructure;
using Xunit;

public class SqlConnectionBuilderTests
{
    [Fact]
    public void Add_AccumulatesDescriptors()
    {
        var builder = new SqlConnectionBuilder();

        builder.Add("Source", "ConnectionStrings:Source", "SourceCred");
        builder.Add("Target", "ConnectionStrings:Target", "TargetCred");

        Assert.Equal(2, builder.Descriptors.Count);
    }

    [Fact]
    public void Add_StoresCorrectValues()
    {
        var builder = new SqlConnectionBuilder();

        builder.Add("Source", "ConnectionStrings:Source", "SourceCred");
        var d = builder.Descriptors[0];

        Assert.Equal("Source",                   d.ServiceKey);
        Assert.Equal("ConnectionStrings:Source", d.ConnectionStringKey);
        Assert.Equal("SourceCred",               d.CredentialKey);
    }

    [Fact]
    public void Add_IsChainable()
    {
        var builder = new SqlConnectionBuilder();

        var returned = builder.Add("A", "ConnectionStrings:A", "ACred");

        Assert.Same(builder, returned);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail (build error expected)**

```bash
cd /Users/ericziko/-🏦gitHub/ericziko/ez-crib-notes/dapper-etl
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj \
  --filter "FullyQualifiedName~SqlConnectionBuilderTests" 2>&1 | tail -15
```

Expected: Build error — `SqlConnectionBuilder` and `SqlConnectionDescriptor` do not exist yet.

- [ ] **Step 3: Create SqlConnectionDescriptor**

Create `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionDescriptor.cs`:

```csharp
namespace Dapper.ETL.Orchestrator.Infrastructure;

public sealed record SqlConnectionDescriptor(
    string ServiceKey,
    string ConnectionStringKey,
    string CredentialKey);
```

- [ ] **Step 4: Create SqlConnectionBuilder**

Create `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionBuilder.cs`:

```csharp
namespace Dapper.ETL.Orchestrator.Infrastructure;

public sealed class SqlConnectionBuilder
{
    internal List<SqlConnectionDescriptor> Descriptors { get; } = new();

    public SqlConnectionBuilder Add(string serviceKey, string connectionStringKey, string credentialKey)
    {
        Descriptors.Add(new SqlConnectionDescriptor(serviceKey, connectionStringKey, credentialKey));
        return this;
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```bash
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj \
  --filter "FullyQualifiedName~SqlConnectionBuilderTests" 2>&1 | tail -8
```

Expected: `3 passed`.

- [ ] **Step 6: Commit**

```bash
git add Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionDescriptor.cs \
        Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionBuilder.cs \
        Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs
git commit -m "Add SqlConnectionDescriptor and SqlConnectionBuilder data classes"
```

---

## Task 2: AssembleConnectionString (core logic)

**Files:**
- Create: `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionExtensions.cs`
- Modify: `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs`

The credential property on `SqlConnectionStringBuilder` is located via reflection: find the public instance property whose name starts with `"Pa"` and ends with `"ord"` and is of type `string`. This avoids the literal trigger word anywhere in source. If the credential config key is absent or blank, `IntegratedSecurity = true` is set instead.

- [ ] **Step 1: Add failing tests for AssembleConnectionString**

Append to `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs`:

```csharp
using Dapper.ETL.Orchestrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

public class AssembleConnectionStringTests
{
    private static IConfiguration Cfg(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void WithCredential_CredentialIsApplied()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["MyCred"] = "s3cr3t",
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MyCred");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.False(builder.IntegratedSecurity);
    }

    [Fact]
    public void WithoutCredential_UsesIntegratedSecurity()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MissingCred");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.True(builder.IntegratedSecurity);
    }

    [Fact]
    public void MissingBaseConnectionString_ThrowsInvalidOperationException()
    {
        var config = Cfg(new() { ["MyCred"] = "s3cr3t" });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SqlConnectionExtensions.AssembleConnectionString(
                config, "ConnectionStrings:Source", "MyCred"));

        Assert.Contains("ConnectionStrings:Source", ex.Message);
    }

    [Fact]
    public void Result_IsValidSqlConnectionString()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["MyCred"] = "s3cr3t",
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MyCred");

        var ex = Record.Exception(() => new SqlConnectionStringBuilder(result));
        Assert.Null(ex);
    }

    [Fact]
    public void Result_ContainsExpectedDatabase()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=MyDb;Encrypt=false",
            ["MyCred"] = "s3cr3t",
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MyCred");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal("MyDb", builder.InitialCatalog);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail (build error)**

```bash
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj \
  --filter "FullyQualifiedName~AssembleConnectionStringTests" 2>&1 | tail -15
```

Expected: Build error — `SqlConnectionExtensions` does not exist yet.

- [ ] **Step 3: Create SqlConnectionExtensions with AssembleConnectionString**

Create `Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionExtensions.cs`:

```csharp
namespace Dapper.ETL.Orchestrator.Infrastructure;

using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class SqlConnectionExtensions
{
    // Resolves the credential property on SqlConnectionStringBuilder via reflection
    // to avoid the literal trigger word anywhere in source.
    private static readonly PropertyInfo CredentialProperty =
        typeof(SqlConnectionStringBuilder)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .First(p => p.Name.StartsWith("Pa", StringComparison.Ordinal)
                     && p.Name.EndsWith("ord", StringComparison.Ordinal)
                     && p.PropertyType == typeof(string));

    /// <summary>
    /// Assembles a validated SQL Server connection string from a base connection string key
    /// and a separate credential key in IConfiguration.
    /// Throws <see cref="InvalidOperationException"/> if the base connection string is absent.
    /// Falls back to integrated security when the credential key is absent or blank.
    /// </summary>
    public static string AssembleConnectionString(
        IConfiguration configuration,
        string connectionStringKey,
        string credentialKey)
    {
        var baseConnStr = configuration[connectionStringKey]
            ?? throw new InvalidOperationException(
                $"Required connection string '{connectionStringKey}' is missing from configuration.");

        var connBuilder = new SqlConnectionStringBuilder(baseConnStr);

        var credential = configuration[credentialKey];
        if (string.IsNullOrWhiteSpace(credential))
        {
            connBuilder.IntegratedSecurity = true;
        }
        else
        {
            CredentialProperty.SetValue(connBuilder, credential);
        }

        return connBuilder.ConnectionString;
    }

    /// <summary>
    /// Registers keyed SQL Server connection strings with the DI container.
    /// Logs all non-credential connection string properties at startup.
    /// Fails fast if any base connection string is missing from configuration.
    /// </summary>
    public static IServiceCollection AddKeyedSqlConnections(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger,
        Action<SqlConnectionBuilder> configure)
    {
        var builder = new SqlConnectionBuilder();
        configure(builder);

        foreach (var descriptor in builder.Descriptors)
        {
            var finalConnStr = AssembleConnectionString(
                configuration,
                descriptor.ConnectionStringKey,
                descriptor.CredentialKey);

            LogConnectionProperties(logger, descriptor.ServiceKey, finalConnStr);

            services.AddKeyedSingleton<string>(descriptor.ServiceKey, (_, _) => finalConnStr);
        }

        return services;
    }

    private static void LogConnectionProperties(ILogger logger, string serviceKey, string connectionString)
    {
        logger.LogInformation("Registered SQL connection [{Key}]:", serviceKey);

        var connBuilder = new SqlConnectionStringBuilder(connectionString);
        var credPropName = CredentialProperty.Name;

        var props = typeof(SqlConnectionStringBuilder)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != credPropName
                     && p.GetIndexParameters().Length == 0
                     && p.CanRead);

        foreach (var prop in props)
        {
            var value = prop.GetValue(connBuilder);
            if (value is null) continue;

            var defaultValue = prop.PropertyType.IsValueType
                ? Activator.CreateInstance(prop.PropertyType)
                : null;
            if (Equals(value, defaultValue)) continue;

            logger.LogInformation("  [{Key}] {Property} = {Value}", serviceKey, prop.Name, value);
        }
    }
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj \
  --filter "FullyQualifiedName~AssembleConnectionStringTests" 2>&1 | tail -8
```

Expected: `5 passed`.

- [ ] **Step 5: Commit**

```bash
git add Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionExtensions.cs \
        Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs
git commit -m "Add SqlConnectionExtensions with credential assembly via reflection and keyed DI registration"
```

---

## Task 3: AddKeyedSqlConnections — registration and logging tests

**Files:**
- Modify: `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs`

- [ ] **Step 1: Add failing tests for AddKeyedSqlConnections**

Append to `Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs`:

```csharp
using Dapper.ETL.Orchestrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class AddKeyedSqlConnectionsTests
{
    private static IConfiguration Cfg(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void SingleConnection_ResolvesFromKeyedServices()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["SourceCred"] = "s3cr3t",
        });
        var services = new ServiceCollection();

        services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
            b.Add("Source", "ConnectionStrings:Source", "SourceCred"));

        var provider = services.BuildServiceProvider();
        var connStr = provider.GetRequiredKeyedService<string>("Source");

        Assert.NotNull(connStr);
        Assert.NotEmpty(connStr);
    }

    [Fact]
    public void MultipleConnections_AllResolveByKey()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["ConnectionStrings:Target"] = "Server=localhost;Database=Tgt;Encrypt=false",
            ["SourceCred"] = "s3cr3t",
            ["TargetCred"] = "t4rg3t",
        });
        var services = new ServiceCollection();

        services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
        {
            b.Add("Source", "ConnectionStrings:Source", "SourceCred");
            b.Add("Target", "ConnectionStrings:Target", "TargetCred");
        });

        var provider = services.BuildServiceProvider();
        var sourceStr = provider.GetRequiredKeyedService<string>("Source");
        var targetStr = provider.GetRequiredKeyedService<string>("Target");

        Assert.NotEqual(sourceStr, targetStr);
        Assert.Contains("Src", sourceStr);
        Assert.Contains("Tgt", targetStr);
    }

    [Fact]
    public void MissingBaseConnectionString_ThrowsAtRegistrationTime()
    {
        var config = Cfg(new() { ["SourceCred"] = "s3cr3t" });
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
                b.Add("Source", "ConnectionStrings:Source", "SourceCred")));

        Assert.Contains("ConnectionStrings:Source", ex.Message);
    }

    [Fact]
    public void LogsConnectionProperties_DoesNotThrow()
    {
        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["SourceCred"] = "s3cr3t",
        });
        var services = new ServiceCollection();

        var ex = Record.Exception(() =>
            services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
                b.Add("Source", "ConnectionStrings:Source", "SourceCred")));

        Assert.Null(ex);
    }

    [Fact]
    public void RegisteredConnectionString_DoesNotContainCredentialInLog()
    {
        // Capture log messages via a recording logger
        var log = new List<string>();
        var logger = new RecordingLogger(log);

        var config = Cfg(new()
        {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["SourceCred"] = "SuperSecret99",
        });
        var services = new ServiceCollection();

        services.AddKeyedSqlConnections(config, logger, b =>
            b.Add("Source", "ConnectionStrings:Source", "SourceCred"));

        Assert.DoesNotContain(log, m => m.Contains("SuperSecret99"));
    }
}

// Minimal ILogger implementation for capturing log output in tests
file sealed class RecordingLogger(List<string> messages) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
        => messages.Add(formatter(state, exception));
}
```

- [ ] **Step 2: Run tests to confirm they pass (implementation already written in Task 2)**

```bash
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj \
  --filter "FullyQualifiedName~AddKeyedSqlConnectionsTests" 2>&1 | tail -8
```

Expected: `5 passed`.

- [ ] **Step 3: Run all orchestrator tests to confirm no regressions**

```bash
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj 2>&1 | tail -10
```

Expected: All previously passing tests still pass.

- [ ] **Step 4: Commit**

```bash
git add Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs
git commit -m "Add AddKeyedSqlConnections integration and credential-exclusion-from-logging tests"
```

---

## Task 4: Update appsettings.json and Program.cs

**Files:**
- Modify: `Dapper.ETL.Orchestrator/appsettings.json`
- Modify: `Dapper.ETL.Orchestrator/Program.cs`

**Constraint:** The Serilog MSSqlServer sink is configured at line 28–38 of `Program.cs` — _before_ the DI container is built. We must assemble the Logs connection string early (using `SqlConnectionExtensions.AssembleConnectionString`) so Serilog gets a fully-formed string.

- [ ] **Step 1: Update appsettings.json — remove inline credentials**

Replace the contents of `Dapper.ETL.Orchestrator/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Source": "Server=localhost,1433;Database=TestDbSource;User Id=sa;Encrypt=false;",
    "Target": "Server=localhost,1433;Database=TestDbTarget;User Id=sa;Encrypt=false;",
    "Logs":   "Server=localhost,1433;Database=EtlLogs;User Id=sa;Encrypt=false;"
  },
  "Seq": {
    "Url": "http://localhost:5341"
  },
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

Credential keys (`SourceCred`, `TargetCred`, `LogsCred`) must be supplied at runtime via environment variables or dotnet user-secrets:

```bash
# Environment variables (CI / Docker / production)
export SourceCred="TestPassword123!"
export TargetCred="TestPassword123!"
export LogsCred="TestPassword123!"

# dotnet user-secrets (local development)
cd Dapper.ETL.Orchestrator
dotnet user-secrets set "SourceCred" "TestPassword123!"
dotnet user-secrets set "TargetCred" "TestPassword123!"
dotnet user-secrets set "LogsCred"   "TestPassword123!"
```

- [ ] **Step 2: Update Program.cs — assemble Logs connection string early for Serilog**

In `Program.cs`, replace lines 28–38 (the Serilog MSSqlServer sink setup) with:

```csharp
var logsConnStr = SqlConnectionExtensions.AssembleConnectionString(
    configuration, "ConnectionStrings:Logs", "LogsCred");

if (!string.IsNullOrWhiteSpace(logsConnStr))
{
    loggerConfig = loggerConfig.WriteTo.MSSqlServer(
        connectionString: logsConnStr,
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "Logs",
            SchemaName = "dbo",
            AutoCreateSqlTable = true
        });
}
```

> The `using Dapper.ETL.Orchestrator.Infrastructure;` is already present at line 2 of `Program.cs`.

- [ ] **Step 3: Update Program.cs — register keyed connections in DI**

In `Program.cs`, after the `services.AddLogging(...)` block (around line 72) and before `services.AddSingleton<EtlService>()`, add:

```csharp
// Keyed SQL Server connection strings — assembled from config + separate credential keys
var startupLogger = new Serilog.Extensions.Logging.SerilogLoggerProvider(Log.Logger, dispose: false)
    .CreateLogger("SqlConnections");

services.AddKeyedSqlConnections(configuration, startupLogger, connections =>
{
    connections.Add("Source", "ConnectionStrings:Source", "SourceCred");
    connections.Add("Target", "ConnectionStrings:Target", "TargetCred");
    connections.Add("Logs",   "ConnectionStrings:Logs",   "LogsCred");
});
```

- [ ] **Step 4: Build to verify no errors**

```bash
dotnet build Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj 2>&1 | tail -8
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add Dapper.ETL.Orchestrator/appsettings.json \
        Dapper.ETL.Orchestrator/Program.cs
git commit -m "Wire keyed SQL connections into Program.cs; move credentials out of appsettings.json"
```

---

## Task 5: Update DataService, EtlService and their tests

**Files:**
- Modify: `Dapper.ETL.Orchestrator/Services/DataService.cs`
- Modify: `Dapper.ETL.Orchestrator/Services/EtlService.cs`
- Modify: `Dapper.ETL.Orchestrator.Tests/Commands/RunEtlCommandTests.cs`
- Modify: `Dapper.ETL.Orchestrator.Tests/Commands/SeedSourceCustomersCommandTests.cs`
- Modify: `Dapper.ETL.Orchestrator.Tests/Commands/StatusCommandTests.cs`

- [ ] **Step 1: Update DataService constructor**

Replace `Dapper.ETL.Orchestrator/Services/DataService.cs` lines 1–24 with:

```csharp
namespace Dapper.ETL.Orchestrator.Services;

using Dapper.ETL.Orchestrator.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

public class DataService
{
    private readonly string _sourceConnectionString;
    private readonly string _targetConnectionString;
    private readonly string _logsConnectionString;

    public DataService(
        [FromKeyedServices("Source")] string sourceConnectionString,
        [FromKeyedServices("Target")] string targetConnectionString,
        [FromKeyedServices("Logs")]   string logsConnectionString)
    {
        _sourceConnectionString = sourceConnectionString;
        _targetConnectionString = targetConnectionString;
        _logsConnectionString   = logsConnectionString;
    }
```

The rest of the file (lines 25–87) is unchanged.

- [ ] **Step 2: Update EtlService constructor and SeedCustomers**

In `Dapper.ETL.Orchestrator/Services/EtlService.cs`:

Replace the class fields and constructor (lines 14–27):

```csharp
    private readonly string _sourceConnectionString;
    private readonly ILogger<EtlService> _logger;

    private static readonly string[] FirstNames = ["John", "Jane", "Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Hank"];
    private static readonly string[] LastNames = ["Smith", "Jones", "Taylor", "Brown", "Wilson", "Davis", "Clark", "Lewis", "Walker", "Hall"];

    public EtlService(
        [FromKeyedServices("Source")] string sourceConnectionString,
        ILogger<EtlService> logger)
    {
        _sourceConnectionString = sourceConnectionString;
        _logger = logger;
    }
```

Replace lines 51–55 inside `SeedCustomers`:

```csharp
        var connectionString = _sourceConnectionString;
```

(Remove the old null-check block that read from `_configuration`.)

Remove the `IConfiguration` field declaration and the `using Microsoft.Extensions.Configuration;` import from the file.

Add `using Microsoft.Extensions.DependencyInjection;` at the top.

- [ ] **Step 3: Update RunEtlCommandTests — BuildEtlService helper**

In `Dapper.ETL.Orchestrator.Tests/Commands/RunEtlCommandTests.cs`, replace the `BuildEtlService` static method:

```csharp
    private static EtlService BuildEtlService()
        => new(
            sourceConnectionString: "Server=.;Database=Source;Trusted_Connection=True",
            logger: NullLogger<EtlService>.Instance);
```

Remove the `IConfiguration` import and `ConfigurationBuilder` usage in `BuildEtlService` — they are no longer needed.

- [ ] **Step 4: Update SeedSourceCustomersCommandTests — BuildEtlService helper**

In `Dapper.ETL.Orchestrator.Tests/Commands/SeedSourceCustomersCommandTests.cs`, the `BuildEtlService` method currently builds `EtlService` using the fixture's connection string and `IConfiguration`. Replace it with:

```csharp
    private EtlService BuildEtlService()
        => new(
            sourceConnectionString: _fixture.GetConnectionString("TestDbSource"),
            logger: NullLogger<EtlService>.Instance);
```

Also remove the unused `IConfiguration`/`ConfigurationBuilder` local variable from `BuildEtlService` if present.

- [ ] **Step 5: Update StatusCommandTests — BuildDataService helper**

In `Dapper.ETL.Orchestrator.Tests/Commands/StatusCommandTests.cs`, the `BuildDataService` / `DataService` constructor call currently passes `IConfiguration`. Replace it with direct string arguments:

```csharp
    private DataService BuildDataService()
        => new(
            sourceConnectionString: _fixture.GetConnectionString("TestDbSource"),
            targetConnectionString: _fixture.GetConnectionString("TestDbTarget"),
            logsConnectionString:   _fixture.GetConnectionString("EtlLogs"));
```

Update any place in `StatusCommandTests.cs` that constructs `new DataService(_configuration)` to use `BuildDataService()` instead.

- [ ] **Step 6: Build everything**

```bash
dotnet build Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj \
             Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj 2>&1 | tail -8
```

Expected: `Build succeeded.`

- [ ] **Step 7: Run all orchestrator tests**

```bash
dotnet test Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj 2>&1 | tail -15
```

Expected: All previously passing tests pass. (Integration tests that require a SQL Server container will still pass — they construct services with real connection strings.)

- [ ] **Step 8: Commit**

```bash
git add Dapper.ETL.Orchestrator/Services/DataService.cs \
        Dapper.ETL.Orchestrator/Services/EtlService.cs \
        Dapper.ETL.Orchestrator.Tests/Commands/RunEtlCommandTests.cs \
        Dapper.ETL.Orchestrator.Tests/Commands/SeedSourceCustomersCommandTests.cs \
        Dapper.ETL.Orchestrator.Tests/Commands/StatusCommandTests.cs
git commit -m "Update DataService and EtlService to receive keyed connection strings; update tests"
```

---

## Self-Review

### Spec coverage

| Requirement | Task |
|-------------|------|
| Two or more SQL Server databases | Task 4: registers Source, Target, Logs |
| Base connection string in `ConnectionStrings:*` | Task 2: `connectionStringKey` reads from config |
| Credential stored separately at root IConfiguration | Task 2: `credentialKey` reads from root |
| No word "Password" in code | Task 2: reflection finds prop by `StartsWith("Pa") && EndsWith("ord")` |
| Credential set via reflection | Task 2: `CredentialProperty.SetValue(connBuilder, credential)` |
| Integrated auth when credential absent | Task 2: `connBuilder.IntegratedSecurity = true` |
| SqlConnectionStringBuilder validates the strings | Task 2: `new SqlConnectionStringBuilder(baseConnStr)` throws on invalid input |
| Fail fast at startup on missing connection string | Task 2: `?? throw new InvalidOperationException(...)` fires at `AddKeyedSqlConnections` call time |
| Register any number of keyed connections | Tasks 1–2: `SqlConnectionBuilder.Add(...)` accumulates N entries |
| Log all non-credential properties at startup | Task 2: `LogConnectionProperties` skips `CredentialProperty.Name` |
| Keyed DI as `string` | Task 2: `services.AddKeyedSingleton<string>(...)` |
| Serilog sink gets assembled connection string | Task 4: `AssembleConnectionString` called before `Log.Logger` is built |
| Update existing services | Task 5: `DataService` and `EtlService` use `[FromKeyedServices]` |
| Update existing tests | Task 5: all three test helpers updated |

### Placeholder scan

No TBD, TODO, "similar to", or "appropriate error handling" phrases present.

### Type consistency

- `SqlConnectionDescriptor` record: defined Task 1, consumed Task 2 ✓
- `SqlConnectionBuilder.Descriptors`: `List<SqlConnectionDescriptor>`, defined Task 1, iterated Task 2 ✓
- `AssembleConnectionString(IConfiguration, string, string) → string`: defined Task 2, called Tasks 3 and 4 ✓
- `AddKeyedSqlConnections(IServiceCollection, IConfiguration, ILogger, Action<SqlConnectionBuilder>)`: defined Task 2, called Task 4 ✓
- DI keys `"Source"`, `"Target"`, `"Logs"`: registered Task 4, consumed Task 5 ✓
- `EtlService(string sourceConnectionString, ILogger<EtlService>)`: defined Task 5 Step 2, test helpers updated Task 5 Steps 3–4 ✓
- `DataService(string, string, string)`: defined Task 5 Step 1, test helper updated Task 5 Step 5 ✓
