# Modular ETL Architecture with Dapper

## Overview

A testable, modular ETL system built on Dapper with MediatR integration. Supports batch processing, flexible column mapping, transaction control, and comprehensive observability.

---

## Core Architecture

```
MediatR Handler
    ↓
IEtlOrchestrator
    ├→ ITableCopyService (multiple calls)
    │   ├→ IColumnMapper
    │   ├→ IBatchProcessor
    │   └→ IEtlLogger
    ├→ IStoredProcedureService (multiple calls)
    │   └→ IEtlLogger
    └→ ITransactionManager
        └→ IEtlLogger
```

---

## Interface Definitions

### 1. **IEtlLogger** - Observability Contract
Injected everywhere for consistent logging.

```csharp
public interface IEtlLogger
{
    void LogTableCopyStarted(string sourceTable, string targetTable, int? batchSize);
    void LogTableCopyCompleted(string sourceTable, string targetTable, int rowsCopied, TimeSpan duration);
    void LogTableTruncated(string targetTable);
    void LogStoredProcedureExecuted(string procedureName, int rowsAffected, TimeSpan duration);
    void LogBatchProcessed(string table, int batchNumber, int rowsProcessed);
    void LogError(string context, Exception ex);
}
```

### 2. **IColumnMapper** - Schema Flexibility
Handles both exact schema matches and partial column mappings.

```csharp
public interface IColumnMapper
{
    /// <summary>
    /// Maps source columns to target columns (exact or partial match)
    /// </summary>
    ColumnMapping GetMapping(string sourceTable, string targetTable);
    
    /// <summary>
    /// Gets SQL SELECT clause with mapped columns
    /// </summary>
    string GetSelectClause(string sourceTable, ColumnMapping mapping);
    
    /// <summary>
    /// Gets SQL INSERT clause with target columns
    /// </summary>
    string GetInsertClause(ColumnMapping mapping);
}

public class ColumnMapping
{
    public string SourceTable { get; set; }
    public string TargetTable { get; set; }
    
    /// <summary>
    /// Maps source column names to target column names
    /// If null or missing, uses exact match
    /// </summary>
    public Dictionary<string, string> MappingDictionary { get; set; }
    
    public IReadOnlyList<string> SourceColumns { get; set; }
    public IReadOnlyList<string> TargetColumns { get; set; }
}
```

### 3. **IBatchProcessor** - Large Table Support
Handles chunked reads/writes for memory efficiency.

```csharp
public interface IBatchProcessor
{
    /// <summary>
    /// Process table data in batches
    /// </summary>
    /// <param name="sourceConnection">Source DB connection</param>
    /// <param name="targetConnection">Target DB connection</param>
    /// <param name="sourceQuery">Full SELECT query with mapped columns</param>
    /// <param name="insertQuery">Full INSERT INTO query with parameters</param>
    /// <param name="batchSize">Rows per batch (e.g., 5000)</param>
    /// <returns>Total rows processed</returns>
    Task<int> ProcessInBatchesAsync(
        IDbConnection sourceConnection,
        IDbConnection targetConnection,
        string sourceQuery,
        string insertQuery,
        int batchSize);
}
```

### 4. **ITransactionManager** - Explicit Transaction Control
Gives caller explicit control over transaction scope and rollback.

```csharp
public interface ITransactionManager
{
    /// <summary>
    /// Begin a transaction on the target database
    /// Caller decides when to commit/rollback
    /// </summary>
    Task<IEtlTransaction> BeginTransactionAsync(IDbConnection targetConnection);
}

public interface IEtlTransaction : IAsyncDisposable
{
    IDbTransaction DbTransaction { get; }
    Task CommitAsync();
    Task RollbackAsync();
}
```

### 5. **ITableCopyService** - Individual Table Copy Operation
Orchestrates copying a single table (truncate + copy).

```csharp
public interface ITableCopyService
{
    /// <summary>
    /// Copy one table from source to target
    /// </summary>
    Task<TableCopyResult> CopyTableAsync(
        IDbConnection sourceConnection,
        IDbConnection targetConnection,
        string sourceTable,
        string targetTable,
        IEtlTransaction transaction,
        TableCopyOptions options);
}

public class TableCopyOptions
{
    /// <summary>
    /// Batch size for reading/writing (default: 5000)
    /// </summary>
    public int BatchSize { get; set; } = 5000;
    
    /// <summary>
    /// Optional column mapping for partial schema match
    /// null = exact schema match (all columns)
    /// </summary>
    public Dictionary<string, string> ColumnMapping { get; set; }
}

public class TableCopyResult
{
    public string SourceTable { get; set; }
    public string TargetTable { get; set; }
    public int RowsCopied { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}
```

### 6. **IStoredProcedureService** - SP Execution
Execute stored procedures with configurable parameters.

```csharp
public interface IStoredProcedureService
{
    /// <summary>
    /// Execute a stored procedure on target database
    /// </summary>
    Task<StoredProcedureResult> ExecuteAsync(
        IDbConnection targetConnection,
        IEtlTransaction transaction,
        StoredProcedureDefinition procedure);
}

public class StoredProcedureDefinition
{
    public string ProcedureName { get; set; }
    
    /// <summary>
    /// Optional parameters (null or empty = parameterless)
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; }
    
    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int? CommandTimeoutSeconds { get; set; }
}

public class StoredProcedureResult
{
    public string ProcedureName { get; set; }
    public int RowsAffected { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}
```

### 7. **IEtlOrchestrator** - Main Orchestrator
Coordinates the entire ETL flow with explicit transaction control.

```csharp
public interface IEtlOrchestrator
{
    /// <summary>
    /// Execute complete ETL workflow
    /// Caller has explicit control via callbacks
    /// </summary>
    Task<EtlExecutionResult> ExecuteAsync(
        IDbConnection sourceConnection,
        IDbConnection targetConnection,
        EtlExecutionPlan plan);
}

public class EtlExecutionPlan
{
    /// <summary>
    /// Tables to copy in order
    /// </summary>
    public List<TableCopyDefinition> TableCopies { get; set; }
    
    /// <summary>
    /// Stored procedures to execute after all tables copied
    /// </summary>
    public List<StoredProcedureDefinition> StoredProcedures { get; set; }
}

public class TableCopyDefinition
{
    public string SourceTable { get; set; }
    public string TargetTable { get; set; }
    public int BatchSize { get; set; } = 5000;
    public Dictionary<string, string> ColumnMapping { get; set; }
}

public class EtlExecutionResult
{
    public List<TableCopyResult> TableResults { get; set; }
    public List<StoredProcedureResult> StoredProcedureResults { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public TimeSpan TotalDuration => EndTime - StartTime;
}
```

---

## Implementation Strategy

### ITableCopyService Implementation
```csharp
public class TableCopyService : ITableCopyService
{
    private readonly IColumnMapper _columnMapper;
    private readonly IBatchProcessor _batchProcessor;
    private readonly IEtlLogger _logger;

    public async Task<TableCopyResult> CopyTableAsync(
        IDbConnection sourceConnection,
        IDbConnection targetConnection,
        string sourceTable,
        string targetTable,
        IEtlTransaction transaction,
        TableCopyOptions options)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogTableCopyStarted(sourceTable, targetTable, options.BatchSize);
            
            // 1. Get column mapping
            var mapping = _columnMapper.GetMapping(sourceTable, targetTable, options.ColumnMapping);
            
            // 2. Truncate target
            var truncateSql = $"TRUNCATE TABLE [{targetTable}]";
            await targetConnection.ExecuteAsync(truncateSql, transaction: transaction.DbTransaction);
            _logger.LogTableTruncated(targetTable);
            
            // 3. Build SELECT and INSERT queries
            var selectClause = _columnMapper.GetSelectClause(sourceTable, mapping);
            var sourceQuery = $"SELECT {selectClause} FROM [{sourceTable}]";
            
            var insertClause = _columnMapper.GetInsertClause(mapping);
            var insertQuery = $"INSERT INTO [{targetTable}] ({insertClause}) VALUES (...)";
            
            // 4. Process in batches
            var rowsCopied = await _batchProcessor.ProcessInBatchesAsync(
                sourceConnection,
                targetConnection,
                sourceQuery,
                insertQuery,
                options.BatchSize);
            
            sw.Stop();
            _logger.LogTableCopyCompleted(sourceTable, targetTable, rowsCopied, sw.Elapsed);
            
            return new TableCopyResult
            {
                SourceTable = sourceTable,
                TargetTable = targetTable,
                RowsCopied = rowsCopied,
                Duration = sw.Elapsed,
                Success = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError($"CopyTable({sourceTable}->{targetTable})", ex);
            return new TableCopyResult
            {
                SourceTable = sourceTable,
                TargetTable = targetTable,
                Success = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }
}
```

### IEtlOrchestrator Implementation
```csharp
public class EtlOrchestrator : IEtlOrchestrator
{
    private readonly ITableCopyService _tableCopyService;
    private readonly IStoredProcedureService _spService;
    private readonly ITransactionManager _transactionManager;
    private readonly IEtlLogger _logger;

    public async Task<EtlExecutionResult> ExecuteAsync(
        IDbConnection sourceConnection,
        IDbConnection targetConnection,
        EtlExecutionPlan plan)
    {
        var result = new EtlExecutionResult
        {
            StartTime = DateTime.UtcNow,
            TableResults = new List<TableCopyResult>(),
            StoredProcedureResults = new List<StoredProcedureResult>()
        };

        try
        {
            // Begin transaction - caller has explicit control
            await using var transaction = await _transactionManager.BeginTransactionAsync(targetConnection);
            
            try
            {
                // 1. Copy all tables
                foreach (var tableDef in plan.TableCopies)
                {
                    var tableResult = await _tableCopyService.CopyTableAsync(
                        sourceConnection,
                        targetConnection,
                        tableDef.SourceTable,
                        tableDef.TargetTable,
                        transaction,
                        new TableCopyOptions
                        {
                            BatchSize = tableDef.BatchSize,
                            ColumnMapping = tableDef.ColumnMapping
                        });
                    
                    result.TableResults.Add(tableResult);
                    
                    // Explicit: check result, decide to continue or rollback
                    if (!tableResult.Success)
                    {
                        result.ErrorMessage = $"Table copy failed: {tableResult.ErrorMessage}";
                        await transaction.RollbackAsync();
                        result.Success = false;
                        return result;
                    }
                }
                
                // 2. Execute stored procedures
                foreach (var spDef in plan.StoredProcedures)
                {
                    var spResult = await _spService.ExecuteAsync(
                        targetConnection,
                        transaction,
                        spDef);
                    
                    result.StoredProcedureResults.Add(spResult);
                    
                    if (!spResult.Success)
                    {
                        result.ErrorMessage = $"Stored procedure failed: {spResult.ErrorMessage}";
                        await transaction.RollbackAsync();
                        result.Success = false;
                        return result;
                    }
                }
                
                // All succeeded - commit
                await transaction.CommitAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.ErrorMessage = ex.Message;
                result.Success = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("EtlOrchestrator.ExecuteAsync", ex);
            result.ErrorMessage = ex.Message;
            result.Success = false;
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }
}
```

---

## MediatR Integration

### Request/Response DTOs
```csharp
public class ExecuteEtlCommand : IRequest<ExecuteEtlCommandResponse>
{
    public string SourceConnectionString { get; set; }
    public string TargetConnectionString { get; set; }
    public EtlExecutionPlan Plan { get; set; }
}

public class ExecuteEtlCommandResponse
{
    public bool Success { get; set; }
    public EtlExecutionResult Data { get; set; }
    public string ErrorMessage { get; set; }
}
```

### Handler
```csharp
public class ExecuteEtlCommandHandler : IRequestHandler<ExecuteEtlCommand, ExecuteEtlCommandResponse>
{
    private readonly IEtlOrchestrator _orchestrator;

    public async Task<ExecuteEtlCommandResponse> Handle(ExecuteEtlCommand request, CancellationToken ct)
    {
        using var sourceConn = new SqlConnection(request.SourceConnectionString);
        using var targetConn = new SqlConnection(request.TargetConnectionString);
        
        await sourceConn.OpenAsync(ct);
        await targetConn.OpenAsync(ct);
        
        var result = await _orchestrator.ExecuteAsync(sourceConn, targetConn, request.Plan);
        
        return new ExecuteEtlCommandResponse
        {
            Success = result.Success,
            Data = result,
            ErrorMessage = result.ErrorMessage
        };
    }
}
```

---

## Dependency Injection Setup

```csharp
services
    .AddScoped<IEtlOrchestrator, EtlOrchestrator>()
    .AddScoped<ITableCopyService, TableCopyService>()
    .AddScoped<IStoredProcedureService, StoredProcedureService>()
    .AddScoped<IBatchProcessor, BatchProcessor>()
    .AddScoped<IColumnMapper, ColumnMapper>()
    .AddScoped<ITransactionManager, TransactionManager>()
    .AddScoped<IEtlLogger, EtlLogger>();
```

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Explicit Transaction Control** | Caller decides when to commit/rollback—more control, handles complex scenarios |
| **Separate Services** | TableCopyService, StoredProcedureService, etc. are independently testable |
| **IColumnMapper Abstraction** | Supports both exact and partial schema matches; can be implemented multiple ways |
| **IBatchProcessor** | Handles large tables efficiently; encapsulates chunking logic |
| **IEtlLogger Everywhere** | Consistent observability without spreading logging code |
| **EtlExecutionPlan** | Declarative structure makes it easy to build plans programmatically |
| **Results Objects** | Each operation returns structured result with success, row counts, duration |

---

## Testing Strategy

### TableCopyService Tests (Moq)
```csharp
[Fact]
public async Task CopyTableAsync_WithExactSchemaMatch_CopiesAllRows()
{
    // Arrange
    var mockColumnMapper = new Mock<IColumnMapper>();
    var mockBatchProcessor = new Mock<IBatchProcessor>();
    var mockLogger = new Mock<IEtlLogger>();
    
    mockBatchProcessor
        .Setup(x => x.ProcessInBatchesAsync(It.IsAny<IDbConnection>(), ...))
        .ReturnsAsync(1000);
    
    var service = new TableCopyService(mockColumnMapper.Object, mockBatchProcessor.Object, mockLogger.Object);
    
    // Act
    var result = await service.CopyTableAsync(sourceConn, targetConn, "Source", "Target", transaction, options);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(1000, result.RowsCopied);
    mockBatchProcessor.Verify(x => x.ProcessInBatchesAsync(...), Times.Once);
}
```

### EtlOrchestrator Tests (Moq)
```csharp
[Fact]
public async Task ExecuteAsync_WhenTableCopyFails_RollsBack()
{
    // Arrange
    var mockTableService = new Mock<ITableCopyService>();
    var mockTransaction = new Mock<IEtlTransaction>();
    
    mockTableService
        .Setup(x => x.CopyTableAsync(...))
        .ReturnsAsync(new TableCopyResult { Success = false });
    
    var orchestrator = new EtlOrchestrator(...);
    
    // Act
    var result = await orchestrator.ExecuteAsync(sourceConn, targetConn, plan);
    
    // Assert
    Assert.False(result.Success);
    mockTransaction.Verify(x => x.RollbackAsync(), Times.Once);
}
```

---

## Extensibility Points

1. **Custom IColumnMapper** - For non-standard column mappings (computed columns, type conversions)
2. **Custom IBatchProcessor** - Alternative implementations (parallel processing, custom buffer management)
3. **Custom IEtlLogger** - Integration with Serilog, Application Insights, Datadog
4. **Custom ITransactionManager** - Different isolation levels, timeout strategies
5. **EtlExecutionPlan Builder** - Fluent API for building plans programmatically

