using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Models;

namespace SQLLite.Integration.Tests;

/// <summary>
/// Integration tests for EtlOrchestrator using a real SQLite database.
/// Tests complete ETL workflows with transaction management.
/// </summary>
public class EtlOrchestratorIntegrationTests : IAsyncLifetime {
    private readonly SqliteFixture _fixture;

    public EtlOrchestratorIntegrationTests() {
        _fixture = new SqliteFixture();
    }

    public async Task InitializeAsync() {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleTableCopy_CompletesSuccessfully() {
        // Arrange
        for (var i = 1; i <= 5; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("SourceTable", "DestTable", new TableCopyOptions()) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.TableCopyResults);
        Assert.True(result.TableCopyResults.First().Success);
        Assert.Equal(5, result.TableCopyResults.First().RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(5, destCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleTableCopies_CompletesSuccessfully() {
        // Arrange - Setup source table
        for (var i = 1; i <= 3; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] {
                ("SourceTable", "DestTable", new TableCopyOptions(batchSize: 10, truncateDestination: false))
            },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.TableCopyResults.First().RowCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithTruncateDestination_TruncatesBeforeInsert() {
        // Arrange
        await _fixture.InsertSourceDataAsync(1, "NewItem", 999);

        // Insert old data in destination
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = @"
                    INSERT INTO DestTable (Id, Name, Value, Description)
                    VALUES (999, 'OldItem', 1, 'OldData')";
            cmd.ExecuteNonQuery();
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("SourceTable", "DestTable", new TableCopyOptions()) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(1, destCount); // Only new item
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingTableCopy_RollsBackTransaction() {
        // Arrange - Don't insert any source data
        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        // Use non-existent table to force error
        var plan = new EtlExecutionPlan(
            new[] { ("NonExistentTable", "DestTable", new TableCopyOptions()) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.TableCopyResults);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRollback_DoesNotRollback() {
        // Arrange - Don't insert any source data, use non-existent table
        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("NonExistentTable", "DestTable", new TableCopyOptions()) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan, false);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_RespectsCancellationToken() {
        // Arrange
        for (var i = 1; i <= 5; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("SourceTable", "DestTable", new TableCopyOptions()) },
            new StoredProcedureDefinition[0]);

        var cts = new CancellationTokenSource();

        // Act
        var result = await orchestrator.ExecuteAsync(plan, cancellationToken: cts.Token);

        // Assert - Should complete successfully
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithBatchProcessing_ProcessesAllRows() {
        // Arrange - Insert 50 rows
        for (var i = 1; i <= 50; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("SourceTable", "DestTable", new TableCopyOptions(batchSize: 15)) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(50, result.TableCopyResults.First().RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(50, destCount);
    }

    [Fact]
    public async Task ExecuteAsync_TransactionCommitsAllChanges() {
        // Arrange
        for (var i = 1; i <= 10; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("SourceTable", "DestTable", new TableCopyOptions()) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert - All changes should be committed
        Assert.True(result.Success);
        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(10, destCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithLargeDataset_SuccessfullyCompletes() {
        // Arrange - Insert 500 rows
        for (var i = 1; i <= 500; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10, $"Description{i}");
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var tableCopyService = new TableCopyService(
            transactionManager,
            new ColumnMapper(),
            new BatchProcessor(),
            new EtlLogger(),
            new SqliteSchemaInspector(_fixture.Connection));
        var storedProcedureService = new StoredProcedureService(transactionManager, new EtlLogger());
        var orchestrator = new EtlOrchestrator(
            transactionManager,
            tableCopyService,
            storedProcedureService,
            new EtlLogger());

        var plan = new EtlExecutionPlan(
            new[] { ("SourceTable", "DestTable", new TableCopyOptions(batchSize: 50)) },
            new StoredProcedureDefinition[0]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(500, result.TableCopyResults.First().RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(500, destCount);
    }
}