namespace Dapper.ETL.Tests;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library.Implementation;
using Library.Interfaces;
using Library.Models;
using Moq;
using Xunit;

public class EtlIntegrationTests {
    [Fact]
    public async Task FullEtlWorkflow_WithMockedDatabase_ExecutesSuccessfully() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(x => x.State).Returns(ConnectionState.Closed);

        var transactionManager = new Mock<ITransactionManager>();
        transactionManager.Setup(x => x.Connection).Returns(mockConnection.Object);
        transactionManager.Setup(x => x.CurrentTransaction).Returns((IDbTransaction?)null);

        var logger = new EtlLogger();

        var tableCopyService = new Mock<ITableCopyService>();
        var storedProcedureService = new Mock<IStoredProcedureService>();

        var copyResult = new TableCopyResult(true, "source", "dest", 100, 1000);
        var procResult = new StoredProcedureResult(true, "TestProcedure", 50);

        tableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyResult);

        storedProcedureService.Setup(x => x.ExecuteAsync(It.IsAny<StoredProcedureDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(procResult);

        var orchestrator = new EtlOrchestrator(
            transactionManager.Object,
            tableCopyService.Object,
            storedProcedureService.Object,
            logger);

        var plan = new EtlExecutionPlan(
            tableCopies: [
                ("SourceTable1", "DestTable1", new TableCopyOptions()),
                ("SourceTable2", "DestTable2", new TableCopyOptions())
            ],
            storedProcedures: [
                new StoredProcedureDefinition("UpdateProcedure"),
                new StoredProcedureDefinition("CleanupProcedure")
            ]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TableCopyResults.Count);
        Assert.Equal(2, result.StoredProcedureResults.Count);
        Assert.All(result.TableCopyResults, r => Assert.True(r.Success));
        Assert.All(result.StoredProcedureResults, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task EtlWithMultipleTables_ProcessesInOrder() {
        // Arrange
        var executionOrder = new List<string>();

        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(x => x.State).Returns(ConnectionState.Closed);

        var transactionManager = new Mock<ITransactionManager>();
        transactionManager.Setup(x => x.Connection).Returns(mockConnection.Object);
        transactionManager.Setup(x => x.CurrentTransaction).Returns((IDbTransaction?)null);

        var logger = new EtlLogger();

        var tableCopyService = new Mock<ITableCopyService>();
        var storedProcedureService = new Mock<IStoredProcedureService>();

        tableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TableCopyOptions, CancellationToken>((src, _, _, _) => { executionOrder.Add($"Copy {src}"); })
            .ReturnsAsync(new TableCopyResult(true, "source", "dest", 100, 1000));

        storedProcedureService.Setup(x => x.ExecuteAsync(It.IsAny<StoredProcedureDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<StoredProcedureDefinition, CancellationToken>((proc, _) => { executionOrder.Add($"Execute {proc.ProcedureName}"); })
            .ReturnsAsync(new StoredProcedureResult(true, "proc", 10));

        var orchestrator = new EtlOrchestrator(
            transactionManager.Object,
            tableCopyService.Object,
            storedProcedureService.Object,
            logger);

        var plan = new EtlExecutionPlan(
            tableCopies: [
                ("Table1", "Table1_Staging", new TableCopyOptions()),
                ("Table2", "Table2_Staging", new TableCopyOptions())
            ],
            storedProcedures: [
                new StoredProcedureDefinition("MergeData"),
                new StoredProcedureDefinition("UpdateMetadata")
            ]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal("Copy Table1", executionOrder[0]);
        Assert.Equal("Copy Table2", executionOrder[1]);
        Assert.Equal("Execute MergeData", executionOrder[2]);
        Assert.Equal("Execute UpdateMetadata", executionOrder[3]);
    }

    [Fact]
    public void ColumnMapperIntegration_WithComplexSchema_MapsCorrectly() {
        // Arrange
        var mapper = new ColumnMapper();

        var sourceColumns = new[] { "Id", "FirstName", "LastName", "Email", "CreatedAt", "UpdatedAt", "IsActive" };
        var destinationColumns = new[] { "UserId", "FirstName", "LastName", "EmailAddress", "CreatedDate", "ModifiedDate", "Active" };
        var overrides = new Dictionary<string, string> {
            { "Id", "UserId" },
            { "Email", "EmailAddress" },
            { "CreatedAt", "CreatedDate" },
            { "UpdatedAt", "ModifiedDate" },
            { "IsActive", "Active" }
        };

        // Act
        var mappings = mapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

        // Assert
        Assert.Equal(7, mappings.Count);
        Assert.Contains(mappings, m => m is { SourceColumn: "Id", DestinationColumn: "UserId" });
        Assert.Contains(mappings, m => m is { SourceColumn: "FirstName", DestinationColumn: "FirstName" });
        Assert.Contains(mappings, m => m is { SourceColumn: "Email", DestinationColumn: "EmailAddress" });
    }

    [Fact]
    public async Task BatchProcessor_WithLargeDataset_ProcessesAllBatches() {
        // Arrange
        var processor = new BatchProcessor();
        var items = Enumerable.Range(1, 10000).ToList();
        var processedCount = 0;
        var batchCounts = new List<int>();

        // Act
        await processor.ProcessInBatchesAsync(
            items,
            1000,
            async (batch, _, _) => {
                processedCount += batch.Count;
                batchCounts.Add(batch.Count);
                await Task.CompletedTask;
            });

        // Assert
        Assert.Equal(10000, processedCount);
        Assert.Equal(10, batchCounts.Count);
        Assert.All(batchCounts, count => Assert.Equal(1000, count));
    }

    [Fact]
    public async Task TransactionManager_WithAsyncOperations_MaintainsConnection() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();

        mockConnection.Setup(x => x.State).Returns(ConnectionState.Closed);
        mockConnection.Setup(x => x.Open()).Callback(() => mockConnection.Setup(x => x.State).Returns(ConnectionState.Open));
        mockConnection.Setup(x => x.BeginTransaction(It.IsAny<IsolationLevel>())).Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.BeginTransactionAsync();
        var transaction = manager.CurrentTransaction;
        await manager.CommitAsync();

        // Assert
        Assert.NotNull(transaction);
        mockConnection.Verify(x => x.BeginTransaction(It.IsAny<IsolationLevel>()), Times.Once);
        mockTransaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public async Task EtlOrchestrator_WithRollbackScenario_ProperlyRollsBack() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();

        mockConnection.Setup(x => x.State).Returns(ConnectionState.Closed);
        mockConnection.Setup(x => x.Open()).Callback(() => mockConnection.Setup(x => x.State).Returns(ConnectionState.Open));
        mockConnection.Setup(x => x.BeginTransaction(It.IsAny<IsolationLevel>())).Returns(mockTransaction.Object);

        var transactionManager = new Mock<ITransactionManager>();
        transactionManager.Setup(x => x.Connection).Returns(mockConnection.Object);
        transactionManager.Setup(x => x.CurrentTransaction).Returns(mockTransaction.Object);

        var tableCopyService = new Mock<ITableCopyService>();
        var storedProcedureService = new Mock<IStoredProcedureService>();
        var logger = new EtlLogger();

        var failureResult = new TableCopyResult(false, "source", "dest", 0, 1000, "Copy failed");
        tableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        var orchestrator = new EtlOrchestrator(
            transactionManager.Object,
            tableCopyService.Object,
            storedProcedureService.Object,
            logger);

        var plan = new EtlExecutionPlan(
            tableCopies: [
                ("source", "dest", new TableCopyOptions())
            ]);

        // Act
        var result = await orchestrator.ExecuteAsync(plan, shouldRollback: true);

        // Assert
        Assert.False(result.Success);
        transactionManager.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}