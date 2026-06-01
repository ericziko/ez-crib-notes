---
uid: 458f59f9-ca49-46da-8eb2-0866a9584e2c
---
# Keyed SQL Server Connection Strings

This document describes how `SqlConnectionExtensions` works and how to register, configure, and consume keyed SQL Server connection strings in the DI container.

---

## Overview

The feature splits connection string configuration into two parts:

1. **Base connection string** — stored under `ConnectionStrings:*` in `appsettings.json`. Contains everything except the credential (server, database, user ID, encryption settings, etc.).
2. **Credential** — stored separately at the root of `IConfiguration`, typically supplied at runtime via environment variables or `dotnet user-secrets`. Never committed to source control.

At startup, `AddKeyedSqlConnections` assembles the full connection string, validates it with `SqlConnectionStringBuilder`, logs all non-sensitive properties, and registers the result as a keyed `string` singleton in the DI container.

---

## Configuration

### appsettings.json

Store base connection strings **without** the credential:

```json
{
  "ConnectionStrings": {
    "Source": "Server=localhost,1433;Database=MySourceDb;User Id=sa;Encrypt=false;",
    "Target": "Server=localhost,1433;Database=MyTargetDb;User Id=sa;Encrypt=false;",
    "Logs":   "Server=localhost,1433;Database=MyLogsDb;User Id=sa;Encrypt=false;"
  }
}
```

### Credential (environment variables — recommended for CI/production)

```bash
export SourceCred="your-source-db-secret"
export TargetCred="your-target-db-secret"
export LogsCred="your-logs-db-secret"
```

### Credential (dotnet user-secrets — recommended for local development)

```bash
cd src/Dapper.ETL.Orchestrator
dotnet user-secrets init          # first time only
dotnet user-secrets set "SourceCred" "your-source-db-secret"
dotnet user-secrets set "TargetCred" "your-target-db-secret"
dotnet user-secrets set "LogsCred"   "your-logs-db-secret"
```

### Integrated security (no credential)

If a credential key is absent or blank in `IConfiguration`, the assembler automatically sets `IntegratedSecurity = true`. This is useful for Windows/domain-joined environments:

```json
{
  "ConnectionStrings": {
    "Source": "Server=.;Database=MySourceDb;Encrypt=false;"
  }
}
```

```bash
# No SourceCred set → falls back to integrated auth
```

---

## Registration

### Basic — register all connections at startup

In `Program.cs`, call `AddKeyedSqlConnections` after logging is configured:

```csharp
using Dapper.ETL.Orchestrator.Infrastructure;

// Each Add() call maps:
//   serviceKey        → the DI key used to resolve the connection string
//   connectionStringKey → config path to the base connection string
//   credentialKey     → config path to the credential (root-level IConfiguration key)
services.AddKeyedSqlConnections(configuration, startupLogger, connections =>
{
    connections.Add("Source", "ConnectionStrings:Source", "SourceCred");
    connections.Add("Target", "ConnectionStrings:Target", "TargetCred");
    connections.Add("Logs",   "ConnectionStrings:Logs",   "LogsCred");
});
```

> **Fail-fast:** if `ConnectionStrings:Source` (or any registered base key) is missing from configuration, `AddKeyedSqlConnections` throws `InvalidOperationException` immediately — before the app starts accepting work.

### Serilog sink (early assembly before DI is built)

The Serilog MSSqlServer sink must be configured before `IServiceCollection` is built. Use the public `AssembleConnectionString` helper directly:

```csharp
var logsConnStr = SqlConnectionExtensions.AssembleConnectionString(
    configuration, "ConnectionStrings:Logs", "LogsCred");

if (!string.IsNullOrWhiteSpace(logsConnStr))
{
    loggerConfig = loggerConfig.WriteTo.MSSqlServer(
        connectionString: logsConnStr,
        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true });
}
```

### Adding more databases

To register a fourth database (e.g. an audit database), add one line to `appsettings.json` and one `Add()` call:

```json
"ConnectionStrings": {
    "Audit": "Server=localhost,1433;Database=AuditDb;User Id=sa;Encrypt=false;"
}
```

```csharp
connections.Add("Audit", "ConnectionStrings:Audit", "AuditCred");
```

```bash
export AuditCred="your-audit-db-secret"
```

No other code changes are required.

---

## Consuming registered connections

Inject the keyed `string` using `[FromKeyedServices("key")]`:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class MyRepository
{
    private readonly string _connStr;

    public MyRepository([FromKeyedServices("Source")] string connStr)
    {
        _connStr = connStr;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        await using var connection = new SqlConnection(_connStr);
        return await connection.QueryAsync<Customer>("SELECT * FROM dbo.Customer");
    }
}
```

For services that need multiple databases:

```csharp
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
}
```

---

## Startup logging

When the app starts, every registered connection logs all non-sensitive `SqlConnectionStringBuilder` properties at `Information` level. The credential, the full `ConnectionString` value, and the `Values` collection are excluded.

Example startup output:

```
[INF] Registered SQL connection [Source]:
[INF]   [Source] DataSource = localhost,1433
[INF]   [Source] InitialCatalog = MySourceDb
[INF]   [Source] UserID = sa
[INF]   [Source] Encrypt = False
[INF] Registered SQL connection [Target]:
[INF]   [Target] DataSource = localhost,1433
[INF]   [Target] InitialCatalog = MyTargetDb
...
```

---

## Testing

### Unit tests — no SQL Server required

Pass a connection string directly to the constructor. The `[FromKeyedServices]` attribute is ignored outside of a DI container:

```csharp
var etlService = new EtlService(
    sourceConnectionString: "Server=.;Database=Source;Trusted_Connection=True",
    logger: NullLogger<EtlService>.Instance);
```

### Integration tests — real SQL Server via Testcontainers

Use the fixture's `GetConnectionString` helper, which returns the fully-assembled container connection string:

```csharp
var dataService = new DataService(
    _fixture.GetConnectionString("TestDbSource"),
    _fixture.GetConnectionString("TestDbTarget"),
    _fixture.GetConnectionString("EtlLogs"));
```

### Testing the assembler in isolation

```csharp
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
        ["MyCred"] = "s3cr3t",
    })
    .Build();

var connStr = SqlConnectionExtensions.AssembleConnectionString(
    config, "ConnectionStrings:Source", "MyCred");

// connStr is a valid, fully-assembled SQL Server connection string
var builder = new SqlConnectionStringBuilder(connStr);
Assert.Equal("Src", builder.InitialCatalog);
```

---

## Key files

| File | Purpose |
|------|---------|
| `src/Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionDescriptor.cs` | Immutable record: DI key + config keys |
| `src/Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionBuilder.cs` | Fluent accumulator — call `.Add()` for each database |
| `src/Dapper.ETL.Orchestrator/Infrastructure/SqlConnectionExtensions.cs` | `AssembleConnectionString` + `AddKeyedSqlConnections` extension |
| `src/Dapper.ETL.Orchestrator/Program.cs` | Registration site |
| `src/Dapper.ETL.Orchestrator/appsettings.json` | Base connection strings (no credentials) |
| `tests/Dapper.ETL.Orchestrator.Tests/Infrastructure/SqlConnectionExtensionsTests.cs` | Unit tests |
