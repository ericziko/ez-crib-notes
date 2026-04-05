---
uid: d3cbac83-46d3-40b8-a230-41b8f38d3c69
---
# FAQ_AND_EXAMPLES

Frequently asked questions and comprehensive working examples for the ISchemaInspector abstraction.

---

## Table of Contents

1. [FAQ](<#faq>)
2. [Detailed Examples](<#detailed-examples>)
3. [Code Patterns](<#code-patterns>)
4. [Migration Guide](<#migration-guide>)

---

## FAQ

### Q1: Why does my SQL Server code fail with "Incorrect syntax near the keyword 'PRAGMA'"?

**A:** Your code is using `SqliteSchemaInspector` with a SQL Server connection. SQLite's `PRAGMA` syntax doesn't exist in SQL Server.

**Fix:**
```csharp
// ❌ WRONG
var inspector = new SqliteSchemaInspector(sqlServerConnection);  // Wrong!

// ✅ CORRECT
var inspector = new SqlServerSchemaInspector(sqlServerConnection);  // Right type
```

Or better, use DI which auto-selects the right implementation:
```csharp
services.AddEtlServices();  // Auto-registers SqlServerSchemaInspector
var service = provider.GetRequiredService<ITableCopyService>();
```

---

### Q2: Can I use ISchemaInspector directly, or must I go through TableCopyService?

**A:** You can use it directly for any schema introspection needs:

```csharp
var inspector = new SqlServerSchemaInspector(connection);
var columns = await inspector.GetColumnNamesAsync("MyTable");

foreach (var column in columns)
{
    Console.WriteLine($"Column: {column}");
}
```

However, the primary use case is through `TableCopyService` in the `EtlOrchestrator`.

---

### Q3: What if I have a table with reserved SQL keywords as column names?

**A:** The inspector just returns column names as strings. It's up to `TableCopyService` and `ColumnMapper` to handle quoting.

```csharp
// Example: table with "select" and "from" columns
var columns = await inspector.GetColumnNamesAsync("BadTable");
// Returns: ["select", "from", "value"]

// TableCopyService will quote these when building queries:
// INSERT INTO "BadTable" ("select", "from", "value") VALUES (...)
```

The quoting is already handled by `TableCopyService` (lines 105, 113, 120).

---

### Q4: Does ISchemaInspector support schema-qualified table names (e.g., "dbo.MyTable")?

**A:** For **SQL Server**, yes - `INFORMATION_SCHEMA.COLUMNS` searches across all schemas:

```csharp
var columns = await sqlServerInspector.GetColumnNamesAsync("dbo.MyTable");
var columns = await sqlServerInspector.GetColumnNamesAsync("MyTable");
// Both work, will find columns
```

For **SQLite**, no schema support (SQLite doesn't have schemas):

```csharp
var columns = await sqliteInspector.GetColumnNamesAsync("dbo.MyTable");
// ❌ Will fail - SQLite doesn't understand "dbo."
```

**Recommendation:** Don't use schema qualifiers; they're handled differently per database. Use just the table name.

---

### Q5: Why is the same ITransactionManager.Connection used for both data copy and schema inspection?

**A:** Transaction consistency.

If you're inside a transaction:
```csharp
connection.BeginTransaction();

var inspector = new SqlServerSchemaInspector(connection);  // ← Same connection
var columns = await inspector.GetColumnNamesAsync("MyTable");

// If you created temp tables in the transaction, the inspector can see them
// If you rolled back the transaction, the inspector will see the rollback
```

The DI registration ensures this:
```csharp
services.AddTransient<ISchemaInspector>(sp =>
    new SqlServerSchemaInspector(
        sp.GetRequiredService<ITransactionManager>().Connection));  // ← Same connection
```

---

### Q6: Can I cache schema lookups to improve performance?

**A:** Yes, you can wrap the inspector in a caching decorator:

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
        if (_cache.ContainsKey(tableName))
        {
            return _cache[tableName];
        }

        var columns = await _inner.GetColumnNamesAsync(tableName);
        _cache[tableName] = columns;
        return columns;
    }

    public void ClearCache() => _cache.Clear();
}
```

Then register it:
```csharp
services.AddTransient<ISchemaInspector>(sp =>
{
    var innerInspector = new SqlServerSchemaInspector(
        sp.GetRequiredService<ITransactionManager>().Connection);
    return new CachingSchemaInspector(innerInspector);
});
```

---

### Q7: What happens if a table doesn't exist?

**A:** The inspector returns an empty list:

```csharp
var columns = await inspector.GetColumnNamesAsync("NonExistentTable");
// Returns: []  (empty list)

// TableCopyService catches this:
if (mappingsList.Count == 0)
{
    return new TableCopyResult(false, sourceTable, destinationTable, 0, 
        stopwatch.ElapsedMilliseconds, "No matching columns between source and destination.");
}
```

---

### Q8: Can I extend ISchemaInspector with new methods?

**A:** Yes, you can create a derived interface:

```csharp
public interface IExtendedSchemaInspector : ISchemaInspector
{
    Task<List<string>> GetPrimaryKeyColumnsAsync(string tableName);
    Task<List<(string Name, string Type)>> GetColumnTypesAsync(string tableName);
}

// Implementation
public class ExtendedSqlServerSchemaInspector : IExtendedSchemaInspector
{
    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        // ... existing implementation
    }

    public async Task<List<string>> GetPrimaryKeyColumnsAsync(string tableName)
    {
        var sql = @"
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
            WHERE TABLE_NAME = @TableName AND CONSTRAINT_NAME LIKE 'PK_%'";
        return (await _connection.QueryAsync<string>(sql, new { TableName = tableName })).ToList();
    }

    public async Task<List<(string Name, string Type)>> GetColumnTypesAsync(string tableName)
    {
        var sql = @"
            SELECT COLUMN_NAME, DATA_TYPE 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = @TableName 
            ORDER BY ORDINAL_POSITION";
        var result = await _connection.QueryAsync<dynamic>(sql, new { TableName = tableName });
        return result.Select(r => ((string)r.COLUMN_NAME, (string)r.DATA_TYPE)).ToList();
    }
}
```

---

### Q9: What's the difference between using a mock vs. real SQLite in tests?

**A:** 

| Aspect | Mock | Real SQLite |
|--------|------|------------|
| **Speed** | Very fast (no DB calls) | ~100-500ms per test |
| **Realism** | Low (fake data) | High (real schema introspection) |
| **Isolation** | Perfect (no side effects) | Good (new DB per test) |
| **What you test** | Business logic | Integration + logic |
| **Best for** | Unit tests | Integration tests |

**Unit tests use mocks:**
```csharp
var mockInspector = new Mock<ISchemaInspector>();
mockInspector.Setup(x => x.GetColumnNamesAsync("SourceTable"))
    .ReturnsAsync(new List<string> { "Id", "Name" });

// Tests TableCopyService logic, not schema introspection
```

**Integration tests use real SQLite:**
```csharp
var inspector = new SqliteSchemaInspector(_fixture.Connection);

// Tests entire flow: schema introspection + column mapping + data copy
var result = await service.CopyTableAsync("SourceTable", "DestTable", options);
```

---

### Q10: If I add a new database (PostgreSQL), what files do I need to change?

**A:** Minimal changes:

1. **Create new implementation** (1 file):
   ```csharp
   public class PostgreSqlSchemaInspector : ISchemaInspector { ... }
   ```

2. **Update DI registration** (1 method, optional):
   ```csharp
   // Optional: Auto-detect by connection type
   services.AddTransient<ISchemaInspector>(sp =>
   {
       var connection = sp.GetRequiredService<ITransactionManager>().Connection;
       return connection switch
       {
           NpgsqlConnection => new PostgreSqlSchemaInspector(connection),
           _ => new SqlServerSchemaInspector(connection)
       };
   });
   ```

3. **Add tests** (1-2 test files, optional):
   ```csharp
   public class PostgreSqlSchemaInspectorTests { ... }
   ```

**That's it!** No changes to `TableCopyService` or any existing code.

---

## Detailed Examples

### Example 1: Complete ETL Workflow (SQL Server)

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper.ETL.Library;
using Dapper.ETL.Library.Models;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main()
    {
        // Setup
        var services = new ServiceCollection();
        services.AddEtlServices();  // ✅ Includes ISchemaInspector

        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IEtlOrchestrator>();

        // Define ETL plan
        var plan = new EtlExecutionPlan(
            tableCopies: new[]
            {
                // Copy with default options
                ("dbo.Customers", "warehouse.Customers", new TableCopyOptions()),

                // Copy with batch processing
                ("dbo.Orders", "warehouse.Orders", new TableCopyOptions(batchSize: 1000)),

                // Truncate destination before copy
                ("dbo.Products", "warehouse.Products", 
                    new TableCopyOptions(truncateDestination: true, batchSize: 500))
            },
            storedProcedures: new[]
            {
                new StoredProcedureDefinition("UpdateWarehouseStats", new Dictionary<string, object>())
            });

        // Execute
        var result = await orchestrator.ExecuteAsync(plan);

        // Report
        Console.WriteLine($"ETL Execution: {(result.Success ? "SUCCESS" : "FAILED")}");
        
        foreach (var copyResult in result.TableCopyResults)
        {
            var status = copyResult.Success ? "✓" : "✗";
            Console.WriteLine(
                $"{status} {copyResult.SourceTable} → {copyResult.DestinationTable}: " +
                $"{copyResult.RowCount} rows in {copyResult.DurationMs}ms");
            
            if (!copyResult.Success)
            {
                Console.WriteLine($"  Error: {copyResult.ErrorMessage}");
            }
        }

        foreach (var procResult in result.StoredProcedureResults)
        {
            var status = procResult.Success ? "✓" : "✗";
            Console.WriteLine($"{status} {procResult.StoredProcedureName}: {procResult.Message}");
        }
    }
}
```

**Output:**
```
ETL Execution: SUCCESS
✓ dbo.Customers → warehouse.Customers: 1,234 rows in 45ms
✓ dbo.Orders → warehouse.Orders: 56,789 rows in 234ms
✓ dbo.Products → warehouse.Products: 890 rows in 23ms
✓ UpdateWarehouseStats: Executed successfully
```

---

### Example 2: Integration Test with Real SQLite

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Models;
using Xunit;

namespace Dapper.ETL.Tests
{
    public class CompleteIntegrationScenario : IAsyncLifetime
    {
        private readonly SqliteFixture _fixture;

        public CompleteIntegrationScenario()
        {
            _fixture = new SqliteFixture();
        }

        public async Task InitializeAsync() => await _fixture.InitializeAsync();
        public async Task DisposeAsync() => await _fixture.DisposeAsync();

        [Fact]
        public async Task FullEtlWorkflow_CopiesDataWithTransactionManagement()
        {
            // Arrange: Insert test data
            for (int i = 1; i <= 100; i++)
            {
                await _fixture.InsertSourceDataAsync(
                    id: i,
                    name: $"Item_{i:D3}",
                    value: i * 10,
                    description: $"Test item {i}");
            }

            // Arrange: Create services
            var transactionManager = new TransactionManager(_fixture.Connection);
            var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);  // ← Real SQLite
            var columnMapper = new ColumnMapper();
            var batchProcessor = new BatchProcessor();
            var logger = new EtlLogger();

            var tableCopyService = new TableCopyService(
                transactionManager,
                columnMapper,
                batchProcessor,
                logger,
                schemaInspector);  // ← Uses real schema introspection

            var storedProcService = new StoredProcedureService(
                transactionManager,
                logger);

            var orchestrator = new EtlOrchestrator(
                transactionManager,
                tableCopyService,
                storedProcService,
                logger);

            // Arrange: Define plan
            var plan = new EtlExecutionPlan(
                tableCopies: new[]
                {
                    ("SourceTable", "DestTable", new TableCopyOptions(batchSize: 25))
                },
                storedProcedures: new StoredProcedureDefinition[0]);

            // Act
            var result = await orchestrator.ExecuteAsync(plan);

            // Assert
            Assert.True(result.Success, $"ETL failed: {result.ErrorMessage}");
            Assert.Single(result.TableCopyResults);
            
            var copyResult = result.TableCopyResults[0];
            Assert.True(copyResult.Success);
            Assert.Equal(100, copyResult.RowCount);
            Assert.Equal("SourceTable", copyResult.SourceTable);
            Assert.Equal("DestTable", copyResult.DestinationTable);
            Assert.True(copyResult.DurationMs > 0);

            // Verify data was actually copied
            var destCount = await _fixture.GetDestTableCountAsync();
            Assert.Equal(100, destCount);

            // Verify data integrity
            using (var cmd = _fixture.Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM DestTable WHERE Value > 500";
                var highValueCount = (long)cmd.ExecuteScalar();
                Assert.Equal(50, highValueCount);  // 500/10, 600/10, ... 1000/10
            }
        }

        [Fact]
        public async Task MultipleTableCopies_WithDifferentBatchSizes()
        {
            // This test would verify that different batch sizes work correctly
            // Insert data, set up services, define multi-table plan, verify results
            
            // Arrange
            var transactionManager = new TransactionManager(_fixture.Connection);
            var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);
            
            var service = new TableCopyService(
                transactionManager,
                new ColumnMapper(),
                new BatchProcessor(),
                new EtlLogger(),
                schemaInspector);

            // Insert different amounts of data
            for (int i = 1; i <= 50; i++)
                await _fixture.InsertSourceDataAsync(i, $"Item{i}", i);

            // Act with different batch sizes
            var result1 = await service.CopyTableAsync("SourceTable", "DestTable",
                new TableCopyOptions(batchSize: 10));
            
            // Assert
            Assert.True(result1.Success);
            Assert.Equal(50, result1.RowCount);
        }
    }
}
```

---

### Example 3: Unit Tests with Mocks

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Interfaces;
using Dapper.ETL.Library.Models;
using Moq;
using Xunit;

namespace Dapper.ETL.Tests
{
    public class TableCopyServiceUnitTests
    {
        [Fact]
        public async Task CopyTableAsync_WithValidInput_CallsSchemaInspectorForSourceAndDest()
        {
            // Arrange
            var mockTransactionManager = new Mock<ITransactionManager>();
            var mockColumnMapper = new Mock<IColumnMapper>();
            var mockBatchProcessor = new Mock<IBatchProcessor>();
            var mockLogger = new Mock<IEtlLogger>();
            var mockSchemaInspector = new Mock<ISchemaInspector>();

            // Setup schema inspector to return columns
            mockSchemaInspector.Setup(x => x.GetColumnNamesAsync("SourceTable"))
                .ReturnsAsync(new List<string> { "Id", "Name", "Value" });

            mockSchemaInspector.Setup(x => x.GetColumnNamesAsync("DestTable"))
                .ReturnsAsync(new List<string> { "Id", "Name", "Value" });

            var service = new TableCopyService(
                mockTransactionManager.Object,
                mockColumnMapper.Object,
                mockBatchProcessor.Object,
                mockLogger.Object,
                mockSchemaInspector.Object);

            // Act
            await service.CopyTableAsync("SourceTable", "DestTable", new TableCopyOptions());

            // Assert
            mockSchemaInspector.Verify(
                x => x.GetColumnNamesAsync("SourceTable"),
                Times.Once,
                "Schema inspector should be called for source table");

            mockSchemaInspector.Verify(
                x => x.GetColumnNamesAsync("DestTable"),
                Times.Once,
                "Schema inspector should be called for destination table");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CopyTableAsync_WithInvalidSourceTable_ThrowsArgumentException(string sourceTable)
        {
            // Arrange
            var mockSchemaInspector = new Mock<ISchemaInspector>();
            var service = new TableCopyService(
                new Mock<ITransactionManager>().Object,
                new Mock<IColumnMapper>().Object,
                new Mock<IBatchProcessor>().Object,
                new Mock<IEtlLogger>().Object,
                mockSchemaInspector.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CopyTableAsync(sourceTable, "dest", new TableCopyOptions()));
        }

        [Fact]
        public async Task CopyTableAsync_WhenSchemaInspectorReturnsEmpty_ReturnsFailure()
        {
            // Arrange
            var mockSchemaInspector = new Mock<ISchemaInspector>();
            
            // Return empty list for both calls
            mockSchemaInspector.Setup(x => x.GetColumnNamesAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string>());

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
            Assert.Contains("No matching columns", result.ErrorMessage);
        }
    }
}
```

---

## Code Patterns

### Pattern 1: Dependency Injection with Auto-Selection

```csharp
// Automatically select inspector based on connection type
services.AddTransient<ISchemaInspector>(sp =>
{
    var connection = sp.GetRequiredService<ITransactionManager>().Connection;
    
    return connection switch
    {
        Microsoft.Data.SqlClient.SqlConnection =>
            new SqlServerSchemaInspector(connection),
        
        Microsoft.Data.Sqlite.SqliteConnection =>
            new SqliteSchemaInspector(connection),
        
        _ => throw new NotSupportedException(
            $"Database type {connection.GetType().Name} not supported")
    };
});
```

### Pattern 2: Decorator for Caching

```csharp
public class CachingSchemaInspectorDecorator : ISchemaInspector
{
    private readonly ISchemaInspector _inner;
    private readonly Dictionary<string, List<string>> _cache;

    public CachingSchemaInspectorDecorator(ISchemaInspector inner)
    {
        _inner = inner;
        _cache = new Dictionary<string, List<string>>();
    }

    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        if (_cache.TryGetValue(tableName, out var cached))
        {
            return cached;
        }

        var columns = await _inner.GetColumnNamesAsync(tableName);
        _cache[tableName] = columns;
        return columns;
    }
}

// Register with decorator
services.AddTransient<ISchemaInspector>(sp =>
{
    var innerInspector = new SqlServerSchemaInspector(
        sp.GetRequiredService<ITransactionManager>().Connection);
    
    return new CachingSchemaInspectorDecorator(innerInspector);
});
```

### Pattern 3: Logging Decorator

```csharp
public class LoggingSchemaInspectorDecorator : ISchemaInspector
{
    private readonly ISchemaInspector _inner;
    private readonly ILogger<LoggingSchemaInspectorDecorator> _logger;

    public LoggingSchemaInspectorDecorator(
        ISchemaInspector inner,
        ILogger<LoggingSchemaInspectorDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        _logger.LogInformation($"Inspecting schema for table: {tableName}");
        
        try
        {
            var columns = await _inner.GetColumnNamesAsync(tableName);
            _logger.LogInformation($"Found {columns.Count} columns in {tableName}");
            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to inspect schema for {tableName}");
            throw;
        }
    }
}
```

---

## Migration Guide

### From Old (Hardcoded) to New (ISchemaInspector)

**Step 1: Update Production Code**

```csharp
// ❌ OLD
new TableCopyService(tm, mapper, processor, logger);

// ✅ NEW - Option 1: Use DI
services.AddEtlServices();
var service = provider.GetRequiredService<ITableCopyService>();

// ✅ NEW - Option 2: Manual
var inspector = new SqlServerSchemaInspector(tm.Connection);
new TableCopyService(tm, mapper, processor, logger, inspector);
```

**Step 2: Update Unit Tests**

```csharp
// ❌ OLD
var service = new TableCopyService(tm, mapper, processor, logger);

// ✅ NEW
var mockInspector = new Mock<ISchemaInspector>();
var service = new TableCopyService(tm, mapper, processor, logger, mockInspector.Object);
```

**Step 3: Update Integration Tests**

```csharp
// ❌ OLD
var service = new TableCopyService(tm, mapper, processor, logger);

// ✅ NEW
var inspector = new SqliteSchemaInspector(_fixture.Connection);
var service = new TableCopyService(tm, mapper, processor, logger, inspector);
```

**That's it!** No logic changes needed.

---

## Quick Reference

| Task | Code |
|------|------|
| **Get columns from SQL Server** | `new SqlServerSchemaInspector(conn)` |
| **Get columns from SQLite** | `new SqliteSchemaInspector(conn)` |
| **Mock in unit test** | `new Mock<ISchemaInspector>()` |
| **Register in DI** | `services.AddEtlServices()` |
| **Call directly** | `var cols = await inspector.GetColumnNamesAsync("Table")` |
| **Use in service** | Pass to `TableCopyService` constructor (5th param) |

