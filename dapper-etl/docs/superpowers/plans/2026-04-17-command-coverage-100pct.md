---
uid: 8821e672-b18f-4275-8f72-f0426eb1633e
---
# Plan: 100% Command Class Coverage

**Date:** 2026-04-17  
**Goal:** Achieve 100% line coverage on all 14 command classes in `Dapper.ETL.Orchestrator`.

---

## Root Cause

Every existing test calls the *underlying service* directly (e.g. `etlService.RunEtl(mode)`) instead of calling `command.Execute(context, settings)`. As a result all command classes sit at **0%** even though the services they call are well-tested.

The fix is simple: instantiate each command and call `Execute()`.

---

## Key Infrastructure to Add First

### 1. `CommandTestFixtures.cs` — shared context factory

`CommandContext` requires an `IRemainingArguments`. Extract once:

```csharp
internal static class CommandTestFixtures
{
    internal static CommandContext CreateContext(string name = "test")
    {
        var remaining = new Mock<IRemainingArguments>();
        remaining.Setup(r => r.Raw).Returns(Array.Empty<string>());
        remaining.Setup(r => r.Parsed)
                 .Returns(new LookupDictionary<string, string>());
        return new CommandContext(remaining.Object, name, null);
    }
}
```

> **Note:** Verify the exact `CommandContext` constructor signature from `Spectre.Console.Cli` source before writing this helper.

### 2. AnsiConsole capture pattern

Commands use the **static** `AnsiConsole`. Redirect it to capture output for Verify.NET snapshots:

```csharp
var sw = new StringWriter();
AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
{
    Ansi        = AnsiSupport.No,
    ColorSystem = ColorSystemSupport.NoColors,
    Interactive = InteractionSupport.No,
    Out         = new AnsiConsoleOutput(sw),
});
command.Execute(context);
var output = sw.ToString();
await Verify(output);
```

If `AnsiConsole.Console` setter is internal/unavailable, add `Spectre.Console.Testing` package and use `new TestConsole()` instead.

### 3. Verify.NET snapshot workflow

- First run creates `.verified.txt` files — run `dotnet test` then `dotnet verify accept` (or approve in IDE).
- Store `.verified.txt` files alongside test files in source control.
- Use `UseDirectory("Snapshots")` per test class to keep things tidy.

---

## Phases — Ordered Easiest → Hardest

### Phase 1 — Infrastructure *(do this first)*

| Task | File |
|---|---|
| Create `CommandTestFixtures.cs` with `CreateContext()` and `CreateTestConsole()` | `Dapper.ETL.Orchestrator.Tests/` |
| Verify `CommandContext` constructor signature compiles | — |

---

### Phase 2 — Pure output commands (no DB, no file I/O)

`DefaultCommand.Execute()` and `DryRunCommand.Execute()` never actually touch their injected service. Easiest tests in the solution.

#### `DefaultCommand`
- `Execute_ReturnsZero` — assert return code = 0
- `Execute_WithVerify_RendersCommandTable` — Verify snapshot of console output

#### `DryRunCommand`
- `Execute_ReturnsZero` — assert return code = 0
- `Execute_WithVerify_RendersDryRunPlan` — Verify snapshot

---

### Phase 3 — MetricsService unit tests (no DB)

`MetricsService` only needs a `Meter` — no SQL required.

#### `GetStatsCommand`
- `Execute_WithNoMetrics_ReturnsZero` — null metrics → yellow "No ETL run recorded" path
- `Execute_WithMetrics_ReturnsZero` — populated metrics → renders table, return 0
- `Execute_WithVerify_RendersMetricsTable` — Verify snapshot

#### `ExportMetricsCommand`
- `Execute_JsonFormat_WritesFileAndReturnsZero` — verify JSON file written + Verify file content
- `Execute_CsvFormat_WritesFileAndReturnsZero` — verify CSV file written + Verify file content
- `Execute_NoMetrics_Returns1` — null/empty metrics → return 1
- `Execute_UnknownFormat_Returns1` — bad format string → return 1

---

### Phase 4 — EtlService unit tests (stub, no real DB needed)

`EtlService.RunEtl()` is a stub that always succeeds with 0 rows (confirmed by existing tests). Use it directly.

#### `RunEtlCommand`
- `Execute_AtomicMode_ReturnsZero` — `settings.Atomic = true`, assert return 0
- `Execute_PartialMode_ReturnsZero` — `settings.Atomic = false`, assert return 0
- `Execute_WithVerify_RendersSuccessOutput` — Verify snapshot

> `SeedSourceCustomersCommand` calls `EtlService.SeedCustomers()` which hits a real DB → deferred to Phase 7.

---

### Phase 5 — DataService integration tests

Requires `SharedSqlServerFixture` (already wired up in existing tests).

#### `StatusCommand`
- `Execute_WithData_ReturnsZero` — seed rows, call `Execute()`, assert 0
- `Execute_WithEmptyDatabase_ReturnsZero` — truncate all, call `Execute()`, assert 0
- `Execute_OnDbException_Returns1` — bad connection string → assert 1

#### `ResetTargetDatabaseCommand`
- `Execute_ResetsAllTables_ReturnsZero` — seed data, reset, assert 0 + verify rows gone

---

### Phase 6 — ValidationService integration tests

`ValidateDataCommand` builds `ValidationService` internally via `new ValidationService(configuration)`, so pass a real `IConfiguration` built from the fixture connection strings.

#### `CompareDataCommand`
- `Execute_WithMatchingData_ReturnsZero` — equal row counts → return 0
- `Execute_WithMismatch_Returns1` — unequal counts → return 1
- `Execute_MismatchesOnlyFlag_FiltersTable` — `MismatchesOnly = true` filters OK rows

#### `ValidateDataCommand`
- `Execute_QuickLevel_ReturnsZero`
- `Execute_StandardLevel_ReturnsZero`
- `Execute_ThoroughLevel_ReturnsZero`
- `Execute_InvalidLevel_FallsBackToQuick` — unknown level uses quick path

---

### Phase 7 — LoggingService & EtlService integration tests

#### `ExportLogsCommand`
- `Execute_WithLogs_WritesJsonFile` — insert logs, export, Verify file content
- `Execute_EmptyLogs_WritesEmptyArray` — no logs → writes `[]`
- `Execute_OnDbException_Returns1` — bad connection → return 1

#### `ShowLogsCommand`  
(`ShowLogsCommand` builds `LoggingService` internally — pass real `IConfiguration`)
- `Execute_WithLogs_ReturnsZero`
- `Execute_FiltersByLevel_ReturnsZero`
- `Execute_RespectsLimit_ReturnsZero`
- `Execute_OnDbException_Returns1`

#### `SeedSourceCustomersCommand`
- `Execute_Seeds5Customers_ReturnsZero` — seed 5, assert return 0 + row count
- `Execute_ZeroCount_ReturnsZero` — count = 0 is valid

---

### Phase 8 — Direct SqlConnection commands (IConfiguration-based)

#### `CheckConnectionCommand`
- `Execute_AllConnectionsValid_ReturnsZero` — fixture connections → return 0
- `Execute_MissingConnectionString_Returns1` — empty config → return 1 (MISSING row)
- `Execute_InvalidConnectionString_Returns1` — bad server → return 1 (FAILED row)

#### `ClearLogsCommand`
- `Execute_Cancelled_ReturnsZero` — non-interactive console defaults to "No" → return 0, no truncate
- `Execute_Confirmed_ClearsLogsAndReturnsZero` — integration: interactive console answers "Yes" → truncate verified

> **Note on non-interactive confirm:** `AnsiConsole.Confirm()` in a non-interactive `TestConsole` will throw or return the default. Test the cancel path by setting `Interactive = InteractionSupport.No`.  
> For the confirmed path, inject a `TestConsole` that responds "y\n" via its input stream.

---

### Phase 9 — Remaining coverage gaps (non-command)

These are separate from commands but round out the 100% goal on other classes:

| Class | Current | Gap |
|---|---|---|
| `MetricsService` | 51.7% | `RecordRowsExpected`, `RecordDuration`, `RecordError`, throughput gauge |
| `SqlServerSchemaInspector` | 25% | All SQL Server path integration tests |
| `EtlOrchestrator` | 71.9% | Partial mode failure + rollback branches |
| `SerilogEtlLogger` | 63.3% | Uncovered log method overloads |

---

## Verify.NET Snapshots

Snapshot files live at `Dapper.ETL.Orchestrator.Tests/Snapshots/`. Name format:
```
{TestClass}.{MethodName}.verified.txt
```

On first run, tests fail with "snapshot not found" — run `dotnet test` once, then use the IDE diff viewer or `dotnet verify accept` to accept all initial snapshots.

---

## Acceptance Criteria

- [ ] `dotnet test --collect:"XPlat Code Coverage"` passes all tests
- [ ] `reportgenerator` shows `Dapper.ETL.Orchestrator.Commands.*` all at **100%**
- [ ] All `.verified.txt` snapshot files committed to source control
- [ ] No new test helpers duplicate what `TestDatabaseHelper` already provides
