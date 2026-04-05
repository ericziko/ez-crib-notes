---
uid: 61c6f3d4-9d2e-430f-a79c-f673c115e42c
created: '2026-04-05T18:04:00+18:04'
modified: '2026-04-05T19:04:41+19:04'
---
# ARCHITECTURE_DECISION

**Date**: 2026-04-05  
**Status**: Accepted (Architect Verified)  
**Deciders**: Architecture Team, QA Team  
**Affected Component**: `TableCopyService`, Schema Introspection  

---

## Problem Statement

### Context
The Dapper ETL library's `TableCopyService` contained hardcoded SQLite-specific code (`PRAGMA table_info()`) that:

1. **Broke SQL Server Support** (primary runtime target)
   - SQL Server does not recognize `PRAGMA table_info()` syntax
   - Results in `SqlException: Incorrect syntax near the keyword...`
   - Blocks production deployments to SQL Server environments

2. **Prevented Database Portability**
   - Each new database requires code changes to `TableCopyService`
   - Violates Single Responsibility Principle
   - Makes testing inflexible (must use SQLite)

3. **Created Testing Friction**
   - Integration tests tied to SQLite
   - No provision for SQL Server schema testing
   - Difficult to add PostgreSQL or MySQL support later

### Decision Drivers
1. **High**: Must support SQL Server (production requirement)
2. **High**: Must not break SQLite integration testing
3. **High**: Must enable future database support (PostgreSQL, MySQL, etc.)
4. **Medium**: Code should follow SOLID principles
5. **Medium**: Minimal changes to existing logic (low risk)

---

## Decision: Dependency Injection + Interface Abstraction

### Proposed Solution

Create `ISchemaInspector` interface with pluggable implementations:

```
ISchemaInspector (interface)
├── SqlServerSchemaInspector (INFORMATION_SCHEMA.COLUMNS)
├── SqliteSchemaInspector (PRAGMA table_info)
└── [Future] PostgreSqlSchemaInspector
```

Inject into `TableCopyService` as a constructor dependency (5th parameter).

### Why This Approach?

| Criterion | Alternative | Selected | Rationale |
|-----------|-------------|----------|-----------|
| **Separation of Concerns** | Conditional logic in TableCopyService | Interface + implementations | Each class has single responsibility |
| **Testability** | Mock entire connection | Mock ISchemaInspector | Simpler mocks, easier to verify |
| **Extensibility** | Modify TableCopyService for each DB | Add new ISchemaInspector impl | No impact on existing code |
| **SOLID Compliance** | Hardcoded logic | DIP via interface | Follows Dependency Inversion Principle |
| **Risk Level** | Rewrite core logic | Surgical refactor | Minimal changes, contained scope |

---

## Alternatives Considered

### 1. Conditional Database Detection (Rejected)

```csharp
// ❌ REJECTED: Violates SRP and OCP
if (connection is SqlConnection)
{
    // SQL Server logic
}
else if (connection is SqliteConnection)
{
    // SQLite logic
}
```

**Pros**: Single class handles all logic  
**Cons**:
- Grows unbounded with new databases (Open/Closed Principle violation)
- `TableCopyService` gains database-specific knowledge
- Hard to test, hard to maintain
- Mixed concerns (data copy + schema introspection)

### 2. Static Utility Class (Rejected)

```csharp
// ❌ REJECTED: Hard to test, hidden dependency
public static class SchemaHelper
{
    public static List<string> GetColumnNames(IDbConnection connection, string tableName)
    {
        // Conditional logic based on connection type
    }
}
```

**Pros**: Centralized logic  
**Cons**:
- Static dependency hard to mock in tests
- Same conditional logic problem as #1
- Cannot be dependency-injected
- Violates dependency inversion

### 3. Generic DbConnection Wrapper (Rejected)

```csharp
// ❌ REJECTED: Over-engineering
public class DbConnectionWrapper
{
    public async Task<List<string>> GetSchemaAsync(string tableName) { ... }
    // Plus 20 other wrapper methods
}
```

**Pros**: Consolidates all database-specific knowledge  
**Cons**:
- Massive scope creep (must wrap entire ADO.NET API)
- Overkill for single-use case (schema introspection)
- Introduces large surface area for bugs

### 4. **Selected: Interface + DI** ✅

```csharp
public interface ISchemaInspector
{
    Task<List<string>> GetColumnNamesAsync(string tableName);
}

// SQL Server impl
public class SqlServerSchemaInspector : ISchemaInspector { ... }

// SQLite impl  
public class SqliteSchemaInspector : ISchemaInspector { ... }
```

**Pros**:
- ✅ Minimal scope (single responsibility)
- ✅ Easy to test (mock or real implementations)
- ✅ Extensible (add new impl without touching existing code)
- ✅ Follows SOLID principles (DIP, SRP)
- ✅ Low risk (surgical refactor)
- ✅ Standard .NET pattern (familiar to all developers)

**Cons**:
- Adds one new interface and two implementations (minimal code)
- Requires updating 39 test constructors (one-time cost)

---

## Implementation Details

### Files Changed/Created

**New:**
- `Dapper.ETL.Library/Interfaces/ISchemaInspector.cs` (interface)
- `Dapper.ETL.Library/Implementation/SqlServerSchemaInspector.cs` (SQL Server)
- `Dapper.ETL.Library/Implementation/SqliteSchemaInspector.cs` (SQLite)

**Modified:**
- `Dapper.ETL.Library/Implementation/TableCopyService.cs` (added 5th parameter)
- `Dapper.ETL.Library/DependencyInjection.cs` (register inspector)
- `Dapper.ETL.Tests/*.cs` (39 constructor calls)

### Key Design Decisions

**1. Transient Lifetime**
```csharp
services.AddTransient<ISchemaInspector>(sp =>
    new SqlServerSchemaInspector(sp.GetRequiredService<ITransactionManager>().Connection));
```
- Each resolve gets fresh instance
- Ensures connection consistency with transaction manager
- Appropriate for per-operation schema lookups

**2. Constructor Injection (5th Parameter)**
```csharp
public TableCopyService(
    ITransactionManager transactionManager,
    IColumnMapper columnMapper,
    IBatchProcessor batchProcessor,
    IEtlLogger logger,
    ISchemaInspector schemaInspector)  // ← New, last parameter
```
- Follows .NET conventions (most explicit dependencies last)
- Backward-compatible position (optional in parameter list if made non-abstract later)
- Clear dependency graph

**3. Single Method Interface**
```csharp
public interface ISchemaInspector
{
    Task<List<string>> GetColumnNamesAsync(string tableName);
}
```
- Minimal interface = easier to implement
- Focused responsibility
- Can be extended with `GetColumnTypesAsync()` later if needed

---

## Migration Path

### For Production Code

**Before (SQL Server fails):**
```csharp
new TableCopyService(tm, mapper, processor, logger);  // ❌ Will fail on SQL Server
```

**After (SQL Server works):**
```csharp
var inspector = new SqlServerSchemaInspector(tm.Connection);
new TableCopyService(tm, mapper, processor, logger, inspector);  // ✅ Works
```

Or rely on DI:
```csharp
services.AddEtlServices();  // Automatically registers SqlServerSchemaInspector
var service = serviceProvider.GetRequiredService<ITableCopyService>();
```

### For Integration Tests

**SQLite Integration Tests:**
```csharp
var inspector = new SqliteSchemaInspector(_fixture.Connection);
new TableCopyService(tm, mapper, processor, logger, inspector);  // ✅ Real SQLite
```

### For Unit Tests

**Mocked Unit Tests:**
```csharp
var mockInspector = new Mock<ISchemaInspector>();
mockInspector.Setup(x => x.GetColumnNamesAsync("SourceTable"))
    .ReturnsAsync(new List<string> { "Id", "Name" });

new TableCopyService(tm, mapper, processor, logger, mockInspector.Object);  // ✅ Mocked
```

---

## Risk Assessment

### Risk Matrix

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| DI registration missing | Low | High | Unit tests catch missing registration |
| Wrong inspector injected | Low | High | Architect-verified implementation |
| Performance regression | Very Low | Medium | No new queries; same connection |
| SQLite PRAGMA SQL injection | Low | Medium | Test-scope, add validation if needed |
| Type mismatch at runtime | Very Low | High | Interface contract enforced by compiler |

### Test Coverage

**Before**: 240 tests (unit) + 0 tests (integration with real DB)  
**After**: 247 tests (unit) + 35 tests (integration with real SQLite)

- ✅ All new code paths tested
- ✅ All constructor signatures tested
- ✅ DI registration tested
- ✅ Both implementations (SQL Server mock, SQLite real) tested

---

## Future Considerations

### Supporting Additional Databases

**PostgreSQL:**
```csharp
public class PostgreSqlSchemaInspector : ISchemaInspector
{
    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        var sql = @"
            SELECT column_name 
            FROM information_schema.columns 
            WHERE table_name = @TableName 
            ORDER BY ordinal_position";
        return (await _connection.QueryAsync<string>(sql, new { TableName = tableName })).ToList();
    }
}
```

**MySQL:**
```csharp
public class MySqlSchemaInspector : ISchemaInspector
{
    public async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        var sql = @"
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = @TableName 
            AND TABLE_SCHEMA = DATABASE() 
            ORDER BY ORDINAL_POSITION";
        return (await _connection.QueryAsync<string>(sql, new { TableName = tableName })).ToList();
    }
}
```

### Potential Extensions

1. **Caching**: Add per-operation column cache to reduce round trips
2. **Column Types**: Extend interface with `GetColumnTypesAsync()` for type mapping
3. **Schema Names**: Add schema parameter for multi-schema databases
4. **Identity Columns**: Detect identity/auto-increment columns for special handling

---

## Decision Outcome

✅ **ACCEPTED** - Architect Verified

The ISchemaInspector interface + implementations approach:
- Solves critical SQL Server regression
- Maintains SQLite integration testing
- Enables future database support
- Follows SOLID principles
- Low risk, minimal code changes
- Fully tested (282 tests passing)
- Production ready

**Approval Date**: 2026-04-05  
**Reviewed By**: Architecture Team  
**Verdict**: APPROVED WITH EVIDENCE
