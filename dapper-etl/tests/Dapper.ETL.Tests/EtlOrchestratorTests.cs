using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Interfaces;
using Dapper.ETL.Library.Models;
using Moq;
using Xunit;

namespace Dapper.ETL.Tests;

public class EtlOrchestratorTests {
    private readonly Mock<IEtlLogger> _mockLogger;
    private readonly Mock<IStoredProcedureService> _mockStoredProcedureService;
    private readonly Mock<ITableCopyService> _mockTableCopyService;
    private readonly Mock<ITransactionManager> _mockTransactionManager;
    private readonly EtlOrchestrator _orchestrator;

    public EtlOrchestratorTests() {
        _mockTransactionManager = new Mock<ITransactionManager>();
        _mockTableCopyService = new Mock<ITableCopyService>();
        _mockStoredProcedureService = new Mock<IStoredProcedureService>();
        _mockLogger = new Mock<IEtlLogger>();

        _orchestrator = new EtlOrchestrator(
            _mockTransactionManager.Object,
            _mockTableCopyService.Object,
            _mockStoredProcedureService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullPlan_ThrowsArgumentNullException() {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _orchestrator.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulTableCopy_CommitsTransaction() {
        // Arrange
        var plan = new EtlExecutionPlan(
            new[] {
                ("source", "dest", new TableCopyOptions())
            });

        var copyResult = new TableCopyResult(true, "source", "dest", 100, 1000);

        _mockTableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        _mockTransactionManager.Verify(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionManager.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithTableCopyFailure_RollsBackTransaction() {
        // Arrange
        var plan = new EtlExecutionPlan(
            new[] {
                ("source", "dest", new TableCopyOptions())
            });

        var failureResult = new TableCopyResult(false, "source", "dest", 0, 1000, "Copy failed");

        _mockTableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan, true);

        // Assert
        Assert.False(result.Success);
        _mockTransactionManager.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithStoredProcedureFailure_RollsBackTransaction() {
        // Arrange
        var procedure = new StoredProcedureDefinition("TestProcedure");

        var plan = new EtlExecutionPlan(
            storedProcedures: new[] { procedure });

        var failureResult = new StoredProcedureResult(false, "TestProcedure", 0, "Execution failed");

        _mockStoredProcedureService.Setup(x => x.ExecuteAsync(It.IsAny<StoredProcedureDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan, true);

        // Assert
        Assert.False(result.Success);
        _mockTransactionManager.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleTables_CopiesAllTables() {
        // Arrange
        var plan = new EtlExecutionPlan(
            new[] {
                ("source1", "dest1", new TableCopyOptions()),
                ("source2", "dest2", new TableCopyOptions()),
                ("source3", "dest3", new TableCopyOptions())
            });

        var copyResult = new TableCopyResult(true, "source", "dest", 100, 1000);

        _mockTableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.TableCopyResults.Count);
        _mockTableCopyService.Verify(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleProcedures_ExecutesAllProcedures() {
        // Arrange
        var plan = new EtlExecutionPlan(
            storedProcedures: new[] {
                new StoredProcedureDefinition("Procedure1"),
                new StoredProcedureDefinition("Procedure2"),
                new StoredProcedureDefinition("Procedure3")
            });

        var procResult = new StoredProcedureResult(true, "Procedure", 10);

        _mockStoredProcedureService.Setup(x => x.ExecuteAsync(It.IsAny<StoredProcedureDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(procResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.StoredProcedureResults.Count);
        _mockStoredProcedureService.Verify(x => x.ExecuteAsync(It.IsAny<StoredProcedureDefinition>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPlan_CommitsTransaction() {
        // Arrange
        var plan = new EtlExecutionPlan();

        // Act
        var result = await _orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        _mockTransactionManager.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithTableCopyFailureAndShouldRollbackFalse_DoesNotRollback() {
        // Arrange
        var plan = new EtlExecutionPlan(
            new[] {
                ("source", "dest", new TableCopyOptions())
            });

        var failureResult = new TableCopyResult(false, "source", "dest", 0, 1000, "Copy failed");

        _mockTableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan, false);

        // Assert
        Assert.False(result.Success);
        _mockTransactionManager.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithTablesAndProcedures_ExecutesBothInOrder() {
        // Arrange
        var plan = new EtlExecutionPlan(
            new[] {
                ("source", "dest", new TableCopyOptions())
            },
            new[] {
                new StoredProcedureDefinition("TestProcedure")
            });

        var copyResult = new TableCopyResult(true, "source", "dest", 100, 1000);
        var procResult = new StoredProcedureResult(true, "TestProcedure", 50);

        _mockTableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyResult);

        _mockStoredProcedureService.Setup(x => x.ExecuteAsync(It.IsAny<StoredProcedureDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(procResult);

        // Act
        var result = await _orchestrator.ExecuteAsync(plan);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.TableCopyResults);
        Assert.Single(result.StoredProcedureResults);
        _mockTransactionManager.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsErrorOnFailure() {
        // Arrange
        var plan = new EtlExecutionPlan(
            new[] {
                ("source", "dest", new TableCopyOptions())
            });

        var failureResult = new TableCopyResult(false, "source", "dest", 0, 1000, "Copy failed");

        _mockTableCopyService.Setup(x => x.CopyTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        await _orchestrator.ExecuteAsync(plan);

        // Assert
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }
}