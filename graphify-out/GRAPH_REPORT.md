# Graph Report - .  (2026-05-09)

## Corpus Check
- Large corpus: 431 files · ~243,343 words. Semantic extraction will be expensive (many Claude tokens). Consider running on a subfolder, or use --no-semantic to run AST-only.

## Summary
- 899 nodes · 891 edges · 135 communities (74 shown, 61 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Connection & Logs Commands|Connection & Logs Commands]]
- [[_COMMUNITY_ETL Core Services|ETL Core Services]]
- [[_COMMUNITY_Connection Test & Default Command Tests|Connection Test & Default Command Tests]]
- [[_COMMUNITY_Model Tests|Model Tests]]
- [[_COMMUNITY_BatchProcessor Tests|BatchProcessor Tests]]
- [[_COMMUNITY_Edge Case Tests|Edge Case Tests]]
- [[_COMMUNITY_ColumnMapper Edge Tests|ColumnMapper Edge Tests]]
- [[_COMMUNITY_Service Results Snapshots|Service Results Snapshots]]
- [[_COMMUNITY_TypeRegistrar  DI Plumbing|TypeRegistrar / DI Plumbing]]
- [[_COMMUNITY_TransactionManager Tests|TransactionManager Tests]]
- [[_COMMUNITY_ColumnMapper Core Tests|ColumnMapper Core Tests]]
- [[_COMMUNITY_TableCopyService Comprehensive Tests|TableCopyService Comprehensive Tests]]
- [[_COMMUNITY_Helm Chart Tooling|Helm Chart Tooling]]
- [[_COMMUNITY_Dependency Injection Tests|Dependency Injection Tests]]
- [[_COMMUNITY_EtlOrchestratorService Tests|EtlOrchestratorService Tests]]
- [[_COMMUNITY_Transaction Integration Tests|Transaction Integration Tests]]
- [[_COMMUNITY_Command Settings Classes|Command Settings Classes]]
- [[_COMMUNITY_Logs  Metrics  Dry Commands|Logs / Metrics / Dry Commands]]
- [[_COMMUNITY_EtlLogger Tests|EtlLogger Tests]]
- [[_COMMUNITY_TableCopy Integration Tests|TableCopy Integration Tests]]
- [[_COMMUNITY_Metrics Service|Metrics Service]]
- [[_COMMUNITY_PowerShell dotnet-run Wrapper|PowerShell dotnet-run Wrapper]]
- [[_COMMUNITY_Export Logs  Metrics Tests|Export Logs / Metrics Tests]]
- [[_COMMUNITY_TableCopyService Tests|TableCopyService Tests]]
- [[_COMMUNITY_EtlOrchestrator Integration Tests|EtlOrchestrator Integration Tests]]
- [[_COMMUNITY_EtlOrchestrator Tests|EtlOrchestrator Tests]]
- [[_COMMUNITY_RunEtl & SeedSource Commands|RunEtl & SeedSource Commands]]
- [[_COMMUNITY_StoredProcedureService Tests|StoredProcedureService Tests]]
- [[_COMMUNITY_Default  Status  Reset Commands|Default / Status / Reset Commands]]
- [[_COMMUNITY_Models & Logs Tests|Models & Logs Tests]]
- [[_COMMUNITY_SqliteFixture|SqliteFixture]]
- [[_COMMUNITY_IAsyncLifetime|IAsyncLifetime]]
- [[_COMMUNITY_SqliteFixture|SqliteFixture]]
- [[_COMMUNITY_IDbConnection|IDbConnection]]
- [[_COMMUNITY_RunEtlCommandTests|RunEtlCommandTests]]
- [[_COMMUNITY_SeedSourceCustomersCommandTests|SeedSourceCustomersCommandTests]]
- [[_COMMUNITY_AssembleConnectionStringTests|AssembleConnectionStringTests]]
- [[_COMMUNITY_AddKeyedSqlConnectionsTests|AddKeyedSqlConnectionsTests]]
- [[_COMMUNITY_EtlIntegrationTests|EtlIntegrationTests]]
- [[_COMMUNITY_SerilogEtlLogger|SerilogEtlLogger]]
- [[_COMMUNITY_Mock|Mock]]
- [[_COMMUNITY_SqlServerFixture|SqlServerFixture]]
- [[_COMMUNITY_IEtlLogger|IEtlLogger]]
- [[_COMMUNITY_SerilogEtlLogger|SerilogEtlLogger]]
- [[_COMMUNITY_ValidationService|ValidationService]]
- [[_COMMUNITY_TypeRegistrar|TypeRegistrar]]
- [[_COMMUNITY_TestHelpers|TestHelpers]]
- [[_COMMUNITY_ColumnMappingTransformTests|ColumnMappingTransformTests]]
- [[_COMMUNITY_LoggingService|LoggingService]]
- [[_COMMUNITY_HashSet|HashSet]]
- [[_COMMUNITY_IAsyncDisposable|IAsyncDisposable]]
- [[_COMMUNITY_ObservabilityConfigTests|ObservabilityConfigTests]]
- [[_COMMUNITY_TestDatabaseHelper|TestDatabaseHelper]]
- [[_COMMUNITY_CommandTestFixtures|CommandTestFixtures]]
- [[_COMMUNITY_SqlConnectionBuilderTests|SqlConnectionBuilderTests]]
- [[_COMMUNITY_IColumnMapper|IColumnMapper]]
- [[_COMMUNITY_RecordingLogger|RecordingLogger]]
- [[_COMMUNITY_DataService|DataService]]
- [[_COMMUNITY_ObservabilityConfig|ObservabilityConfig]]
- [[_COMMUNITY_ICollectionFixture|ICollectionFixture]]
- [[_COMMUNITY_Dapper_ETL_AppHost|Dapper_ETL_AppHost]]
- [[_COMMUNITY_Dapper_ETL_Orchestrator|Dapper_ETL_Orchestrator]]
- [[_COMMUNITY_DependencyInjection|DependencyInjection]]
- [[_COMMUNITY_ISchemaInspector|ISchemaInspector]]
- [[_COMMUNITY_ITableCopyService|ITableCopyService]]
- [[_COMMUNITY_IEtlOrchestrator|IEtlOrchestrator]]
- [[_COMMUNITY_IStoredProcedureService|IStoredProcedureService]]
- [[_COMMUNITY_IBatchProcessor|IBatchProcessor]]
- [[_COMMUNITY_SqlConnectionBuilder|SqlConnectionBuilder]]
- [[_COMMUNITY_EtlOptions|EtlOptions]]
- [[_COMMUNITY_TableCopyOptions|TableCopyOptions]]
- [[_COMMUNITY_StoredProcedureDefinition|StoredProcedureDefinition]]
- [[_COMMUNITY_EtlExecutionPlan|EtlExecutionPlan]]
- [[_COMMUNITY_ColumnMapping|ColumnMapping]]
- [[_COMMUNITY_StoredProcedureResult|StoredProcedureResult]]
- [[_COMMUNITY_TableCopyResult|TableCopyResult]]
- [[_COMMUNITY_EtlExecutionResult|EtlExecutionResult]]
- [[_COMMUNITY_EtlResult|EtlResult]]
- [[_COMMUNITY_EtlRunInfo|EtlRunInfo]]
- [[_COMMUNITY_MetricsData|MetricsData]]
- [[_COMMUNITY_ValidationResult|ValidationResult]]

## God Nodes (most connected - your core abstractions)
1. `ModelsTests` - 33 edges
2. `EdgeCaseTests` - 25 edges
3. `ColumnMapperEdgeCasesTests` - 24 edges
4. `ServiceResultsSnapshotTests` - 20 edges
5. `BatchProcessorComprehensiveTests` - 19 edges
6. `TransactionManagerTests` - 18 edges
7. `ColumnMapperTests` - 18 edges
8. `TableCopyServiceComprehensiveTests` - 18 edges
9. `DependencyInjectionTests` - 17 edges
10. `TransactionIntegrationTests` - 16 edges

## Surprising Connections (you probably didn't know these)
- `Compare-HelmCharts()` --calls--> `Get-HelmChartVariables()`  [INFERRED]
  2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Compare-HelmCharts.ps1 → 2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Get-HelmChartVariables.ps1
- `Get-HelmChartVariables()` --calls--> `Get-HelmVariablePattern()`  [INFERRED]
  2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Get-HelmChartVariables.ps1 → 2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Private/Get-HelmVariablePattern.ps1
- `Get-HelmChartStructure()` --calls--> `Get-HelmChartVariables()`  [INFERRED]
  2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Get-HelmChartStructure.ps1 → 2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Get-HelmChartVariables.ps1
- `Export-HelmVariableRegistry()` --calls--> `Get-HelmChartVariables()`  [INFERRED]
  2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Export-HelmVariableRegistry.ps1 → 2026/02/2026-02-24/helm-chart-compare/HelmChartTools/Functions/Public/Get-HelmChartVariables.ps1
- `SqlServerFixture` --references--> `MsSqlContainer`  [EXTRACTED]
  dapper-etl/tests/Dapper.ETL.Orchestrator.Tests/Fixtures/SqlServerFixture.cs → dapper-etl/tests/Dapper.ETL.Orchestrator.Tests/Fixtures/SharedSqlServerFixture.cs

## Communities (135 total, 61 thin omitted)

### Community 0 - "Connection & Logs Commands"
Cohesion: 0.05
Nodes (9): CheckConnectionCommand, ClearLogsCommandTests, CompareDataCommandTests, ShowLogsCommandTests, ValidateDataCommandTests, IConfiguration, EndToEndTests, LoggingService (+1 more)

### Community 1 - "ETL Core Services"
Cohesion: 0.06
Nodes (15): bool, IBatchProcessor, IColumnMapper, IEtlLogger, IEtlOrchestrator, BatchProcessor, ColumnMapper, EtlLogger (+7 more)

### Community 2 - "Connection Test & Default Command Tests"
Cohesion: 0.08
Nodes (7): CheckConnectionCommandTests, DefaultCommandTests, DryRunCommandTests, GetStatsCommandTests, ResetTargetDatabaseCommandTests, StatusCommandTests, SharedSqlServerFixture

### Community 4 - "BatchProcessor Tests"
Cohesion: 0.06
Nodes (3): BatchProcessor, BatchProcessorComprehensiveTests, BatchProcessorTests

### Community 8 - "TypeRegistrar / DI Plumbing"
Cohesion: 0.12
Nodes (8): IDisposable, DisposableService, ITestService, TestService, TypeRegistrarTests, TypeResolver, IServiceProvider, ITypeResolver

### Community 12 - "Helm Chart Tooling"
Cohesion: 0.12
Nodes (9): Compare-FlatYaml(), ConvertTo-FlatYaml(), Get-HelmVariablePattern(), Compare-HelmCharts(), Compare-HelmChartValues(), Export-HelmVariableRegistry(), Get-HelmChartStructure(), Get-HelmChartVariables() (+1 more)

### Community 16 - "Command Settings Classes"
Cohesion: 0.13
Nodes (8): CompareDataSettings, ExportLogsSettings, ExportMetricsSettings, RunEtlSettings, SeedSourceCustomersSettings, ShowLogsSettings, ValidateDataSettings, CommandSettings

### Community 17 - "Logs / Metrics / Dry Commands"
Cohesion: 0.15
Nodes (7): Command, ClearLogsCommand, DryRunCommand, ExportMetricsCommand, GetStatsCommand, double, MetricsService

### Community 20 - "Metrics Service"
Cohesion: 0.14
Nodes (4): Counter, Dictionary, Histogram, MetricsService

### Community 21 - "PowerShell dotnet-run Wrapper"
Cohesion: 0.2
Nodes (7): Find-ProjectFile(), Get-LaunchSettings(), Resolve-SolutionRoot(), Get-DotnetProjects(), Invoke-DotnetRun(), Show-RunConfigurations(), Start-DevApp()

### Community 22 - "Export Logs / Metrics Tests"
Cohesion: 0.22
Nodes (3): ExportLogsCommandTests, ExportMetricsCommandTests, List

### Community 26 - "RunEtl & SeedSource Commands"
Cohesion: 0.18
Nodes (5): RunEtlCommand, SeedSourceCustomersCommand, EtlService, ILogger, EtlService

### Community 28 - "Default / Status / Reset Commands"
Cohesion: 0.2
Nodes (5): DefaultCommand, ResetTargetDatabaseCommand, StatusCommand, DataService, string

### Community 31 - "IAsyncLifetime"
Cohesion: 0.29
Nodes (3): SharedSqlServerFixture, IAsyncLifetime, MsSqlContainer

### Community 33 - "IDbConnection"
Cohesion: 0.28
Nodes (5): IDbConnection, IDbTransaction, SqliteSchemaInspector, SqlServerSchemaInspector, ISchemaInspector

### Community 40 - "Mock"
Cohesion: 0.25
Nodes (3): EtlOrchestratorIntegrationTests, EtlOrchestrator, Mock

### Community 44 - "ValidationService"
Cohesion: 0.29
Nodes (3): CompareDataCommand, ValidateDataCommand, ValidationService

### Community 45 - "TypeRegistrar"
Cohesion: 0.25
Nodes (3): TypeRegistrar, IServiceCollection, ITypeRegistrar

### Community 48 - "LoggingService"
Cohesion: 0.29
Nodes (3): ExportLogsCommand, ShowLogsCommand, LoggingService

### Community 49 - "HashSet"
Cohesion: 0.38
Nodes (3): HashSet, SqlConnectionExtensions, PropertyInfo

## Knowledge Gaps
- **25 isolated node(s):** `TableCopyService`, `SerilogEtlLogger`, `StoredProcedureService`, `EtlLogger`, `SqliteConnection` (+20 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **61 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IConfiguration` connect `Connection & Logs Commands` to `Logs / Metrics / Dry Commands`, `Export Logs / Metrics Tests`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Why does `string` connect `Default / Status / Reset Commands` to `SqliteFixture`, `SqlServerFixture`, `DataService`, `RunEtl & SeedSource Commands`, `IAsyncLifetime`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Why does `ILogger` connect `RunEtl & SeedSource Commands` to `RecordingLogger`, `SerilogEtlLogger`, `Metrics Service`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._
- **What connects `TableCopyService`, `SerilogEtlLogger`, `StoredProcedureService` to the rest of the system?**
  _25 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Connection & Logs Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._
- **Should `ETL Core Services` be split into smaller, more focused modules?**
  _Cohesion score 0.06 - nodes in this community are weakly interconnected._
- **Should `Connection Test & Default Command Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.08 - nodes in this community are weakly interconnected._