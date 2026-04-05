# ISchemaInspector Quick Start Guide

Quick reference for using the ISchemaInspector abstraction in different scenarios.

---

## Table of Contents

1. [Quick Links](#quick-links)
2. [Production Setup (SQL Server)](#production-setup-sql-server)
3. [Integration Testing (SQLite)](#integration-testing-sqlite)
4. [Unit Testing (Mocks)](#unit-testing-mocks)
5. [Common Scenarios](#common-scenarios)
6. [Troubleshooting](#troubleshooting)

---

## Quick Links

| Scenario | File | Key Class |
|----------|------|-----------|
| **Interface** | `Dapper.ETL.Library/Interfaces/ISchemaInspector.cs` | `ISchemaInspector` |
| **SQL Server** | `Dapper.ETL.Library/Implementation/SqlServerSchemaInspector.cs` | `SqlServerSchemaInspector` |
| **SQLite** | `Dapper.ETL.Library/Implementation/SqliteSchemaInspector.cs` | `SqliteSchemaInspector` |
| **Consumer** | `Dapper.ETL.Library/Implementation/TableCopyService.cs` | `TableCopyService` |
| **DI Setup** | `Dapper.ETL.Library/DependencyInjection.cs` | `AddEtlServices()` |

---

## Production Setup (SQL Server)

### Step 1: Register Services

```csharp
// Program.cs or Startup.cs
var services = new ServiceCollection();
services.AddEtlServices();  // ✅ Automatically registers SqlServerSchemaInspector
```

### Step 2: Use the Orchestrator

```csharp
var provider = services.BuildServiceProvider();
var orchestrator = provider.GetRequiredService<IEtlOrchestrator>();

var plan = new EtlExecutionPlan(
    new[] { 
        ("SourceTable", "DestTable", new TableCopyOptions { batchSize: 100 }),
        ("OtherSource", "OtherDest", new TableCopyOptions { truncateDestination: true })
    },
    new StoredProcedureDefinition[0]);

var result = await orchestrator.ExecuteAsync(plan);

foreach (var copyResult in result.TableCopyResults)
{
    Console.WriteLine($"Copied {copyResult.RowCount} rows from {copyResult.SourceTable} to {copyResult.DestinationTable} in {copyResult.DurationMs}ms");
}
```

### Step 3: Done!

The `ISchemaInspector` is automatically injected and uses SQL Server's `INFORMATION_SCHEMA.COLUMNS` to get table column information.

---

## Integration Testing (SQLite)

### Step 1: Set Up Fixture

```csharp
public class TableCopyIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;

    public TableCopyIntegrationTests()
    {
        _fixture = new SqliteFixture();
    }

    public async Task InitializeAsync() => await _fixture.InitializeAsync();
    public async Task DisposeAsync() => await _fixture.DisposeAsync();
```

### Step 2: Create Real SQLite Inspector

```csharp
[Fact]
public async Task CopyTableAsync_WithMultipleRows_CopiesSuccessfully()
{
    // Arrange
    for (int i = 1; i <= 25; i++)
    {
        await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
    }

    var transactionManager = new TransactionManager(_fixture.Connection);
    var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);  // ← Use real SQLite

    var service = new TableCopyService(
        transactionManager,
        new ColumnMapper(),
        new BatchProcessor(),
        new EtlLogger(),
        schemaInspector);  // ← Inject here

    var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

    // Act
    var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

    // Assert
    Assert.True(result.Success);
    Assert.Equal(25, result.RowCount);
}
```

**Key Points:**
- Create `SqliteSchemaInspector` with `_fixture.Connection`
- This uses real SQLite PRAGMA table_info to get columns
- Tests actual schema introspection behavior
- No mocking = high-confidence integration tests

---

## Unit Testing (Mocks)

### Step 1: Mock the Inspector

```csharp
[Fact]
public async Task CopyTableAsync_WithNullSourceTable_ThrowsArgumentException()
{
    // Arrange
    var mockTransactionManager = new Mock<ITransactionManager>();
    var mockColumnMapper = new Mock<IColumnMapper>();
    var mockBatchProcessor = new Mock<IBatchProcessor>();
    var mockLogger = new Mock<IEtlLogger>();
    var mockSchemaInspector = new Mock<ISchemaInspector>();  // ← Mock here

    var service = new TableCopyService(
        mockTransactionManager.Object,
        mockColumnMapper.Object,
        mockBatchProcessor.Object,
        mockLogger.Object,
        mockSchemaInspector.Object);  // ← Pass mock

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(async () =>
        await service.CopyTableAsync(null!, "dest", new TableCopyOptions()));
}
```

### Step 2: Setup Mock Expectations (Optional)

```csharp
[Fact]
public async Task CopyTableAsync_CallsSchemaInspectorForBothTables()
{
    // Arrange
    var mockSchemaInspector = new Mock<ISchemaInspector>();
    
    // Setup mock to return columns
    mockSchemaInspector.Setup(x => x.GetColumnNamesAsync("SourceTable"))
        .ReturnsAsync(new List<string> { "Id", "Name", "Value" });
    
    mockSchemaInspector.Setup(x => x.GetColumnNamesAsync("DestTable"))
        .ReturnsAsync(new List<string> { "Id", "Name", "Value" });

    // ... create service with mock ...

    // Act
    await service.CopyTableAsync("SourceTable", "DestTable", options);

    // Assert - Verify inspector was called for both tables
    mockSchemaInspector.Verify(
        x => x.GetColumnNamesAsync("SourceTable"), 
        Times.Once);
    mockSchemaInspector.Verify(
        x => x.GetColumnNamesAsync("DestTable"), 
        Times.Once);
}
```

**Key Points:**
- Mock allows unit tests to focus on TableCopyService logic
- No database required for unit tests
- Fast, isolated, deterministic tests

---

## Common Scenarios

### Scenario 1: Copying Between SQL Server Tables

```csharp
// Automatic via DI - nothing special needed
services.AddEtlServices();  // SqlServerSchemaInspector registered by default

var orchestrator = serviceProvider.GetRequiredService<IEtlOrchestrator>();
var result = await orchestrator.ExecuteAsync(plan);
```

**Why it works:**
- `SqlServerSchemaInspector` uses `INFORMATION_SCHEMA.COLUMNS`
- ANSI-standard, works on SQL Server 2012+
- Connection string is regular SQL Server connection

### Scenario 2: Integration Test with Truncate

```csharp
[Fact]
public async Task CopyTableAsync_WithTruncateDestination_TruncatesBeforeCopy()
{
    // Insert old data
    using (var cmd = _fixture.Connection.CreateCommand())
    {
        cmd.CommandText = "INSERT INTO DestTable (Id, Name, Value) VALUES (999, 'OldItem', 1)";
        cmd.ExecuteNonQuery();
    }

    var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);
    var service = new TableCopyService(
        new TransactionManager(_fixture.Connection),
        new ColumnMapper(),
        new BatchProcessor(),
        new EtlLogger(),
        schemaInspector);

    var options = new TableCopyOptions(truncateDestination: true);  // ← Truncate enabled

    // Act
    var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

    // Assert
    Assert.True(result.Success);
    var finalCount = await _fixture.GetDestTableCountAsync();
    Assert.Equal(1, finalCount);  // Only new row, old row deleted
}
```

### Scenario 3: Unit Test Error Handling

```csharp
[Fact]
public async Task CopyTableAsync_WhenSchemaInspectorThrows_ReturnsFailure()
{
    var mockSchemaInspector = new Mock<ISchemaInspector>();
    
    // Setup mock to throw exception
    mockSchemaInspector.Setup(x => x.GetColumnNamesAsync(It.IsAny<string>()))
        .ThrowsAsync(new InvalidOperationException("Database error"));

    var service = new TableCopyService(
        new Mock<ITransactionManager>().Object,
        new Mock<IColumnMapper>().Object,
        new Mock<IBatchProcessor>().Object,
        new Mock<IEtlLogger>().Object,
        mockSchemaInspector.Object);

    // Act
    var result = await service.CopyTableAsync("source", "dest", new TableCopyOptions());

    // Assert
    Assert.False(result.Success);
    Assert.NotNull(result.ErrorMessage);
}
```

---

## Troubleshooting

### Issue: `Unable to resolve service for type 'ISchemaInspector'`

**Cause:** `AddEtlServices()` not called

**Solution:**
```csharp
// ❌ WRONG
var services = new ServiceCollection();
services.AddTransient<ITableCopyService, TableCopyService>();  // Missing dependency!

// ✅ CORRECT
var services = new ServiceCollection();
services.AddEtlServices();  // Includes all dependencies
```

### Issue: `NullReferenceException` in SchemaInspector

**Cause:** Connection is null

**Solution:**
```csharp
// ❌ WRONG
var inspector = new SqlServerSchemaInspector(null);  // Will throw

// ✅ CORRECT
var transactionManager = new TransactionManager(connection);
var inspector = new SqlServerSchemaInspector(transactionManager.Connection);
```

### Issue: SQLite `no such table` error

**Cause:** Fixture didn't initialize tables

**Solution:**
```csharp
public class MyIntegrationTests : IAsyncLifetime
{
    private readonly SqliteFixture _fixture;

    public MyIntegrationTests()
    {
        _fixture = new SqliteFixture();
    }

    // ✅ REQUIRED
    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();  // Creates schema
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }
}
```

### Issue: `Incorrect syntax near the keyword 'PRAGMA'` on SQL Server

**Cause:** Wrong inspector injected

**Solution:**
```csharp
// ❌ WRONG
var inspector = new SqliteSchemaInspector(sqlServerConnection);

// ✅ CORRECT
var inspector = new SqlServerSchemaInspector(sqlServerConnection);

// Or let DI handle it:
services.AddEtlServices();  // Auto-selects correct implementation
```

### Issue: Mock not returning data

**Cause:** Setup doesn't match call

**Solution:**
```csharp
// ❌ WRONG
mockInspector.Setup(x => x.GetColumnNamesAsync("MyTable"))
    .ReturnsAsync(new List<string> { "Id" });

await service.CopyTableAsync("DIFFERENT_TABLE", "dest", options);  // Won't match!

// ✅ CORRECT
mockInspector.Setup(x => x.GetColumnNamesAsync(It.IsAny<string>()))
    .ReturnsAsync(new List<string> { "Id", "Name" });

// Or be specific:
mockInspector.Setup(x => x.GetColumnNamesAsync("SourceTable"))
    .ReturnsAsync(new List<string> { "Id", "Name" });

await service.CopyTableAsync("SourceTable", "dest", options);  // Matches!
```

---

## Performance Notes

- **SQL Server**: INFORMATION_SCHEMA.COLUMNS query ~1-5ms
- **SQLite**: PRAGMA table_info ~1-2ms
- **Per-Copy Overhead**: < 10ms for schema lookup
- **Caching**: Not implemented (columns fetched once per copy operation)

For high-volume operations with repeated tables, consider implementing a cache:

```csharp
public class CachingSchemaInspector : ISchemaInspector
{
    private readonly ISchemaInspector _inner;
    private readonly Dictionary<string, List<string>> _cache = new();

    public CachingSchemaInspector(ISchemaInspector inner)
    {
        _inner = inner;
    }

    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        if (_cache.TryGetValue(tableName, out var columns))
        {
            return columns;
        }

        var result = await _inner.GetColumnNamesAsync(tableName);
        _cache[tableName] = result;
        return result;
    }
}
```

---

## Summary

| Use Case | What to Do |
|----------|-----------|
| **Production SQL Server** | Use `AddEtlServices()` - auto-registers SqlServerSchemaInspector |
| **Integration Tests (SQLite)** | Manually create `new SqliteSchemaInspector(connection)` |
| **Unit Tests** | Use `Mock<ISchemaInspector>()` with Moq |
| **Custom Database** | Create new class implementing `ISchemaInspector` |
| **Debugging** | Check that inspector type matches your connection type |

See [SCHEMA_INSPECTOR_GUIDE.md](./SCHEMA_INSPECTOR_GUIDE.md) for detailed examples.  
See [ARCHITECTURE_DECISION.md](./ARCHITECTURE_DECISION.md) for design rationale.
