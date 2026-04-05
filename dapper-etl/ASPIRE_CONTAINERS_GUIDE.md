---
uid: a9d8c7b6-5e4f-4a3c-9b2a-1f8e7d6c5b4a
title: Aspire Containers & Testcontainers Guide
created: 2026-04-05
modified: 2026-04-05
tags:
  - aspire
  - testcontainers
  - docker
  - infrastructure
  - testing
---

# Aspire Containers & Testcontainers Guide

This guide covers the containerized infrastructure for the Dapper ETL project, including local development with .NET Aspire and automated testing with Testcontainers.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Architecture Overview](#architecture-overview)
3. [Local Development with Aspire](#local-development-with-aspire)
4. [Integration Testing with Testcontainers](#integration-testing-with-testcontainers)
5. [Database Schema & Initialization](#database-schema--initialization)
6. [CLI Commands Reference](#cli-commands-reference)
7. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker (for containers)
- Docker CLI available in PATH

### Run the Aspire Host (Local Development)

```bash
cd Dapper.ETL.AppHost
dotnet run
```

**What this does:**
- Starts SQL Server 2022 container with 3 databases (TestDbSource, TestDbTarget, EtlLogs)
- Starts Seq logging container
- Injects connection strings into the ETL.Orchestrator project
- Displays the Aspire dashboard for monitoring

**Connection Details:**
- SQL Server: `localhost:1433`
- Credentials: `sa` / `TestPassword123!`
- Seq: `http://localhost:5341`

### Run Tests with Testcontainers

```bash
dotnet test Dapper.ETL.Orchestrator.Tests
```

**What this does:**
- Automatically spins up ephemeral SQL Server containers per test
- Runs 13 command tests + integration tests
- Cleans up containers after tests complete
- No manual container management needed

---

## Architecture Overview

### Components

```
┌─────────────────────────────────────────────┐
│     Dapper.ETL.AppHost (Aspire Host)        │
│  - Orchestrates containers for local dev    │
│  - Injects connection strings via DI        │
│  - Provides dashboard for monitoring        │
└──────────────┬──────────────────────────────┘
               │
       ┌───────┴────────┐
       │                │
   ┌───▼────────┐  ┌───▼──────┐
   │ SQL Server │  │    Seq   │
   │   2022     │  │  Logging │
   │ Container  │  │Container │
   │(1433)      │  │ (5341)   │
   └────────────┘  └──────────┘
```

### Test Infrastructure

```
┌──────────────────────────────────────────────┐
│  Dapper.ETL.Orchestrator.Tests               │
│  - Uses Testcontainers for SQL Server        │
│  - Automatic container lifecycle management │
│  - Fixtures: SqlServerFixture, TestHelpers  │
└──────────────────────────────────────────────┘
         │
         ├── Fixtures/SqlServerFixture.cs
         │   - Spins up MsSqlContainer
         │   - Executes init.sql scripts
         │   - Provides connection management
         │
         ├── Fixtures/TestDatabaseHelper.cs
         │   - InsertCustomersAsync()
         │   - GetRowCountAsync()
         │   - TruncateTableAsync()
         │
         ├── Integration/EndToEndTests.cs
         │   - Full workflow tests
         │   - Seed → Validate → Compare → Status
         │
         └── Commands/
             - CheckConnectionCommandTests
             - ClearLogsCommandTests
             - CompareDataCommandTests
             - ... (13 total command tests)
```

---

## Local Development with Aspire

### Starting the Aspire Host

```bash
cd Dapper.ETL.AppHost
dotnet run
```

**Expected Output:**
```
Aspire dashboard: http://localhost:18888
SQL Server ready at: localhost:1433
Seq ready at: http://localhost:5341
```

### Accessing the Dashboard

Open `http://localhost:18888` in your browser to:
- Monitor container health
- View logs from SQL Server and Seq
- See resource utilization
- Access Seq dashboard for structured logging

### Connecting Clients

The AppHost injects these environment variables into the ETL.Orchestrator:

```
ConnectionStrings__Source=Server=localhost,1433;Database=TestDbSource;User Id=sa;Password=TestPassword123!;Encrypt=false
ConnectionStrings__Target=Server=localhost,1433;Database=TestDbTarget;User Id=sa;Password=TestPassword123!;Encrypt=false
ConnectionStrings__Logs=Server=localhost,1433;Database=EtlLogs;User Id=sa;Password=TestPassword123!;Encrypt=false
Seq__Url=http://localhost:5341
```

### Running the CLI Manually

```bash
# After Aspire host is running, in another terminal:
cd Dapper.ETL.Orchestrator
dotnet run -- seed-customers 10

# Check logs
dotnet run -- show-logs

# View metrics
dotnet run -- export-metrics
```

### Stopping the Aspire Host

Press `Ctrl+C` in the Aspire terminal. Containers automatically stop and cleanup.

---

## Integration Testing with Testcontainers

### How It Works

1. **Per-Test Container**: Each test class gets its own SQL Server container
2. **Automatic Lifecycle**: Containers start in `InitializeAsync()`, stop in `DisposeAsync()`
3. **Script Execution**: `init.sql` runs automatically to create databases and schema
4. **No Cleanup Needed**: Containers are ephemeral and automatically removed

### Test Structure

```csharp
public class EndToEndTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture = new();
    
    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();  // Container starts + init.sql runs
        _configuration = BuildConfiguration();
    }
    
    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();  // Container stopped + cleaned up
    }
    
    [Fact]
    public async Task Test_Workflow()
    {
        // Use _fixture to get connections
        await using var conn = await _fixture.GetConnectionAsync("TestDbSource");
        var count = await TestDatabaseHelper.GetRowCountAsync(conn, "Customer");
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test Dapper.ETL.Orchestrator.Tests

# Run specific test class
dotnet test Dapper.ETL.Orchestrator.Tests --filter "ClassName=EndToEndTests"

# Run with verbose output
dotnet test Dapper.ETL.Orchestrator.Tests -v detailed

# Collect code coverage
dotnet test Dapper.ETL.Orchestrator.Tests /p:CollectCoverage=true
```

### Test Categories

#### 1. **Integration Tests (EndToEndTests.cs)**
- Full workflow: Seed → Validate → Compare → Status
- Real database operations
- Validates multi-step scenarios

#### 2. **Command Tests (Commands/)**
13 command test classes covering all CLI operations:

| Test Class | Command | Coverage |
|-----------|---------|----------|
| CheckConnectionCommandTests | check-connection | Validates DB connectivity |
| ClearLogsCommandTests | clear-logs | Truncates log tables |
| CompareDataCommandTests | compare-data | Compares source/target rows |
| DryRunCommandTests | dry-run | ETL simulation |
| ExportLogsCommandTests | export-logs | Exports logs to file |
| ExportMetricsCommandTests | export-metrics | Exports metrics to file |
| GetStatsCommandTests | get-stats | Database statistics |
| ResetTargetDatabaseCommandTests | reset-target | Truncates target tables |
| RunEtlCommandTests | run-etl | Executes ETL with transactions |
| SeedSourceCustomersCommandTests | seed-customers | Populates source database |
| ShowLogsCommandTests | show-logs | Displays recent logs |
| StatusCommandTests | status | Reports overall status |
| ValidateDataCommandTests | validate-data | Schema/data validation |

#### 3. **Utility Tests (Utility/TestHelpers.cs)**
- Connection validation
- Row counting
- Table truncation

---

## Database Schema & Initialization

### Schema Overview

**Three Databases:**

1. **TestDbSource** - Source data for ETL
   - `Customer` table: CustomerId (PK), FirstName, LastName, EmailAddress
   - `CustomerIdSequence` for auto-incrementing IDs

2. **TestDbTarget** - Destination for ETL operations
   - `CustomerCopy` - Copy of source customers
   - `CustomerEmailList` - Email extraction with sequence
   - `CustomerLoyaltyRewards` - Enriched loyalty data with sequence

3. **EtlLogs** - Structured logging
   - `Logs` table: Standard Serilog schema
   - Index on TimeStamp for query performance

### Initialization Process

`scripts/init.sql` creates all databases and schema:

```sql
-- Create databases
CREATE DATABASE TestDbSource;
CREATE DATABASE TestDbTarget;
CREATE DATABASE EtlLogs;

-- TestDbSource setup
USE TestDbSource;
CREATE SEQUENCE dbo.CustomerIdSequence START WITH 1;
CREATE TABLE dbo.Customer (
    CustomerId INT NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    EmailAddress NVARCHAR(255) NOT NULL
);

-- TestDbTarget setup (similar)

-- EtlLogs setup
USE EtlLogs;
CREATE TABLE dbo.Logs (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MessageTemplate NVARCHAR(MAX) NOT NULL,
    Level VARCHAR(128) NOT NULL,
    TimeStamp DATETIME2 NOT NULL,
    Exception NVARCHAR(MAX),
    LogEvent NVARCHAR(MAX)
);
CREATE INDEX IX_Logs_TimeStamp ON dbo.Logs(TimeStamp DESC);
```

### Manual Schema Setup (If Needed)

If containers fail to initialize, manually run:

```bash
# Connect to SQL Server
sqlcmd -S localhost -U sa -P TestPassword123!

# Run init script
sqlcmd -S localhost -U sa -P TestPassword123! -i scripts/init.sql
```

---

## CLI Commands Reference

All commands require connection strings (set automatically by Aspire, or provide via environment variables).

### Connection String Setup

**Via Aspire:** Automatic (no action needed)

**Manual:**
```bash
export ConnectionStrings__Source="Server=localhost;Database=TestDbSource;User Id=sa;Password=TestPassword123!;Encrypt=false"
export ConnectionStrings__Target="Server=localhost;Database=TestDbTarget;User Id=sa;Password=TestPassword123!;Encrypt=false"
export ConnectionStrings__Logs="Server=localhost;Database=EtlLogs;User Id=sa;Password=TestPassword123!;Encrypt=false"
export Seq__Url="http://localhost:5341"
```

### Commands

#### 1. **seed-customers** - Populate source database

```bash
dotnet run -- seed-customers 100
# Output: Seeded 100 customers into TestDbSource
```

#### 2. **status** - Check overall ETL status

```bash
dotnet run -- status
# Output: Source: 100 rows | Target: 0 rows | Logs: 0 entries
```

#### 3. **validate-data** - Validate schema and data integrity

```bash
dotnet run -- validate-data
# Output: Schema validation passed | Data validation passed
```

#### 4. **check-connection** - Test database connectivity

```bash
dotnet run -- check-connection
# Output: ✓ Source connected | ✓ Target connected | ✓ Logs connected
```

#### 5. **run-etl** - Execute ETL operation

```bash
dotnet run -- run-etl --mode atomic
# Output: ETL completed | Copied 100 rows | Duration: 250ms
```

#### 6. **compare-data** - Compare source and target

```bash
dotnet run -- compare-data
# Output: Source: 100 rows | Target: 100 rows | Match: Yes
```

#### 7. **dry-run** - Simulate ETL without changes

```bash
dotnet run -- dry-run
# Output: Would copy 100 rows | Would truncate target | (no actual changes)
```

#### 8. **clear-logs** - Truncate log tables

```bash
dotnet run -- clear-logs
# Output: Cleared 156 log entries
```

#### 9. **show-logs** - Display recent logs

```bash
dotnet run -- show-logs --limit 10
# Output: [INFO] ETL started | [INFO] Seeding 100 customers | ...
```

#### 10. **export-logs** - Export logs to file

```bash
dotnet run -- export-logs --output logs.json
# Output: Exported 156 log entries to logs.json
```

#### 11. **export-metrics** - Export performance metrics

```bash
dotnet run -- export-metrics --output metrics.json
# Output: Exported metrics: 5 ETL runs, avg 250ms
```

#### 12. **reset-target** - Truncate target database

```bash
dotnet run -- reset-target
# Output: Reset TestDbTarget | Cleared 100 rows
```

#### 13. **default** - Show help

```bash
dotnet run
# Displays command usage and available options
```

---

## Troubleshooting

### Issue: Aspire Host won't start

**Error:** `Docker daemon not running`

**Solution:**
```bash
# Start Docker daemon
docker daemon  # or use Docker Desktop app

# Verify Docker is working
docker ps
```

### Issue: SQL Server container fails to start

**Error:** `Port 1433 already in use`

**Solution:**
```bash
# Find process using port
lsof -i :1433

# Stop conflicting container
docker stop <container-id>

# Or use different port in AppHost
// Dapper.ETL.AppHost/Program.cs
var sqlServer = builder.AddSqlServer("sql-server", password: sqlPassword, port: 1434);
```

### Issue: Tests timeout waiting for SQL Server

**Error:** `Testcontainers: Container failed to start within the timeout`

**Solution:**
- Increase Docker available memory
- Ensure Docker is not CPU-starved
- Check Docker logs: `docker logs <container-id>`

### Issue: Connection string not injected

**Error:** `NullReferenceException: ConnectionStrings:Source is null`

**Solution:**
- Ensure Aspire host is running: `cd Dapper.ETL.AppHost && dotnet run`
- Or set environment variables manually (see [Connection String Setup](#connection-string-setup))

### Issue: `init.sql` failed to execute

**Error:** `SqlCommandException: Incorrect syntax near 'GO'`

**Solution:**
- The `GO` keyword is batch separator, requires special handling
- `SqlServerFixture.cs` handles this automatically
- If manually running, use `sqlcmd` instead of SSMS

### Issue: Seq logs not appearing

**Error:** Logs in SQL Server but not in Seq dashboard

**Solution:**
```bash
# Verify Seq URL
echo $Seq__Url  # Should be http://localhost:5341

# Check Seq health
curl http://localhost:5341/api/

# Restart Seq container
docker restart <seq-container-id>
```

---

## Performance Tips

### Aspire Host
- Startup time: ~10-15 seconds
- Container overhead: ~500MB RAM (combined SQL Server + Seq)
- Keep host running across multiple CLI runs to avoid restart overhead

### Testcontainers Tests
- Per-test container startup: ~3-5 seconds
- Parallel test execution can help (runs multiple containers)
- Tests typically complete in <1 second each after container startup

### Database Operations
- Seeding 100 customers: ~50ms
- ETL copy (100 rows): ~100-200ms
- Schema inspection: ~1-5ms

---

## Next Steps

1. **Start local development**: `cd Dapper.ETL.AppHost && dotnet run`
2. **Run tests**: `dotnet test Dapper.ETL.Orchestrator.Tests`
3. **Explore CLI**: `cd Dapper.ETL.Orchestrator && dotnet run -- status`
4. **Check logs**: View Seq dashboard at `http://localhost:5341`

See [QUICK_START.md](QUICK_START.md) for the complete development workflow.
