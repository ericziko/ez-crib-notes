using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Interfaces;
using Dapper.ETL.Library.Models;
using Moq;
using Xunit;

namespace Dapper.ETL.Tests;

/// <summary>
/// Integration-style unit tests for EtlOrchestrator transaction mode behaviour.
/// </summary>
public class EtlOrchestratorIntegrationTests {
    private readonly Mock<IEtlLogger> _mockLogger;
    private readonly Mock<IStoredProcedureService> _mockSp;
    private readonly Mock<ITableCopyService> _mockTableCopy;
    private readonly Mock<ITransactionManager> _mockTm;
    private readonly EtlOrchestrator _orchestrator;

    public EtlOrchestratorIntegrationTests() {
        _mockTm = new Mock<ITransactionManager>();
        _mockTableCopy = new Mock<ITableCopyService>();
        _mockSp = new Mock<IStoredProcedureService>();
        _mockLogger = new Mock<IEtlLogger>();

        _orchestrator = new EtlOrchestrator(
            _mockTm.Object,
            _mockTableCopy.Object,
            _mockSp.Object,
            _mockLogger.Object);
    }

    // -----------------------------------------------------------------------
    // Atomic mode
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_AtomicMode_WhenFirstCopyFails_RollsBackAndReturnsFailure() {
        // Arrange - two tables; first fails, second should never be attempted
        var failResult = new TableCopyResult(false, "src1", "dst1", 0, 10, "disk full");
        var successResult = new TableCopyResult(true, "src2", "dst2", 50, 20);

        var callOrder = new List<string>();
        _mockTableCopy
            .SetupSequence(s => s.CopyTableAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult)
            .ReturnsAsync(successResult);

        var plan = new EtlExecutionPlan(new[] {
            ("src1", "dst1", new TableCopyOptions()),
            ("src2", "dst2", new TableCopyOptions())
        });

        // Act
        var result = await _orchestrator.ExecuteAsync(
            plan,
            true,
            EtlTransactionMode.Atomic);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("disk full", result.ErrorMessage);

        // Rollback called once; commit never called
        _mockTm.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTm.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        // Second table copy was never attempted (stopped on first failure)
        _mockTableCopy.Verify(
            s => s.CopyTableAsync("src2", "dst2", It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AtomicMode_WhenAllSucceed_CommitsAndReportsRowCounts() {
        // Arrange
        var r1 = new TableCopyResult(true, "src1", "dst1", 100, 50);
        var r2 = new TableCopyResult(true, "src2", "dst2", 200, 80);

        _mockTableCopy
            .SetupSequence(s => s.CopyTableAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(r1)
            .ReturnsAsync(r2);

        var plan = new EtlExecutionPlan(new[] {
            ("src1", "dst1", new TableCopyOptions()),
            ("src2", "dst2", new TableCopyOptions())
        });

        // Act
        var result = await _orchestrator.ExecuteAsync(
            plan,
            transactionMode: EtlTransactionMode.Atomic);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.TableCopyResults.Count);
        Assert.Equal(100, result.TableCopyResults[0].RowCount);
        Assert.Equal(200, result.TableCopyResults[1].RowCount);

        _mockTm.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTm.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Partial mode
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_PartialMode_WhenFirstCopyFails_ContinuesToSecondAndLogsFailure() {
        // Arrange - first fails, second succeeds
        var failResult = new TableCopyResult(false, "src1", "dst1", 0, 10, "constraint violation");
        var successResult = new TableCopyResult(true, "src2", "dst2", 75, 30);

        _mockTableCopy
            .SetupSequence(s => s.CopyTableAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult)
            .ReturnsAsync(successResult);

        var plan = new EtlExecutionPlan(new[] {
            ("src1", "dst1", new TableCopyOptions()),
            ("src2", "dst2", new TableCopyOptions())
        });

        // Act
        var result = await _orchestrator.ExecuteAsync(
            plan,
            transactionMode: EtlTransactionMode.Partial);

        // Assert: overall failure because one copy failed
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);

        // Both copies were attempted
        Assert.Equal(2, result.TableCopyResults.Count);
        Assert.False(result.TableCopyResults[0].Success);
        Assert.True(result.TableCopyResults[1].Success);
        Assert.Equal(75, result.TableCopyResults[1].RowCount);

        // Logger called for the failure
        _mockLogger.Verify(
            l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_PartialMode_WhenAllSucceed_ReturnsSuccessWithRowCounts() {
        // Arrange
        var r1 = new TableCopyResult(true, "A", "B", 42, 10);
        var r2 = new TableCopyResult(true, "C", "D", 99, 20);

        _mockTableCopy
            .SetupSequence(s => s.CopyTableAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(r1)
            .ReturnsAsync(r2);

        var plan = new EtlExecutionPlan(new[] {
            ("A", "B", new TableCopyOptions()),
            ("C", "D", new TableCopyOptions())
        });

        // Act
        var result = await _orchestrator.ExecuteAsync(
            plan,
            transactionMode: EtlTransactionMode.Partial);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(42, result.TableCopyResults[0].RowCount);
        Assert.Equal(99, result.TableCopyResults[1].RowCount);
    }
}