namespace Dapper.ETL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper.ETL.Library.Implementation;
    using Dapper.ETL.Library.Interfaces;
    using Dapper.ETL.Library.Models;
    using Moq;
    using Xunit;

    /// <summary>
    /// Tests for EtlOrchestrator service
    /// </summary>
    public class EtlOrchestratorServiceTests
    {
        [Fact]
        public void Constructor_WithNullTransactionManager_ThrowsArgumentNullException()
        {
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            Assert.Throws<ArgumentNullException>(() =>
                new EtlOrchestrator(null!, tableCopy.Object, sp.Object, logger.Object));
        }

        [Fact]
        public void Constructor_WithNullTableCopyService_ThrowsArgumentNullException()
        {
            var tm = new Mock<ITransactionManager>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            Assert.Throws<ArgumentNullException>(() =>
                new EtlOrchestrator(tm.Object, null!, sp.Object, logger.Object));
        }

        [Fact]
        public void Constructor_WithNullStoredProcedureService_ThrowsArgumentNullException()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var logger = new Mock<IEtlLogger>();

            Assert.Throws<ArgumentNullException>(() =>
                new EtlOrchestrator(tm.Object, tableCopy.Object, null!, logger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();

            Assert.Throws<ArgumentNullException>(() =>
                new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, null!));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullPlan_ThrowsArgumentNullException()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                orchestrator.ExecuteAsync(null!));
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyPlan_ExecutesSuccessfully()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var plan = new EtlExecutionPlan();
            var result = await orchestrator.ExecuteAsync(plan);

            Assert.True(result.Success);
            Assert.Empty(result.TableCopyResults);
            Assert.Empty(result.StoredProcedureResults);
            tm.Verify(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Once);
            tm.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenTableCopyFails_RollsBackAndReturnsFailure()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(false, "Source", "Dest", 0, 100, "Failed"));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var tableCopies = new List<(string SourceTable, string DestinationTable, TableCopyOptions Options)>
            {
                ("Source", "Dest", new TableCopyOptions())
            };
            var plan = new EtlExecutionPlan(tableCopies, new List<StoredProcedureDefinition>());

            var result = await orchestrator.ExecuteAsync(plan);

            Assert.False(result.Success);
            tm.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenStoredProcedureFails_RollsBackAndReturnsFailure()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(true, "Source", "Dest", 100, 1000));

            sp.Setup(x => x.ExecuteAsync(
                It.IsAny<StoredProcedureDefinition>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StoredProcedureResult(false, "sp_Test", 0, "Failed"));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var plan = new EtlExecutionPlan(
                new List<(string, string, TableCopyOptions)> { ("Source", "Dest", new TableCopyOptions()) },
                new List<StoredProcedureDefinition> { new("sp_Test") });

            var result = await orchestrator.ExecuteAsync(plan);

            Assert.False(result.Success);
            tm.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithShouldRollbackFalse_DoesNotRollback()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(false, "Source", "Dest", 0, 100, "Failed"));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var plan = new EtlExecutionPlan(
                new List<(string, string, TableCopyOptions)> { ("Source", "Dest", new TableCopyOptions()) },
                new List<StoredProcedureDefinition>());

            var result = await orchestrator.ExecuteAsync(plan, shouldRollback: false);

            Assert.False(result.Success);
            tm.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_CompleteFlow_SuccessfullyExecutesAllOperations()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(true, "Source", "Dest", 100, 1000));

            sp.Setup(x => x.ExecuteAsync(
                It.IsAny<StoredProcedureDefinition>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StoredProcedureResult(true, "sp_Update", 50));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var plan = new EtlExecutionPlan(
                new List<(string, string, TableCopyOptions)>
                {
                    ("Users", "Users", new TableCopyOptions()),
                    ("Orders", "Orders", new TableCopyOptions())
                },
                new List<StoredProcedureDefinition>
                {
                    new("sp_Update"),
                    new("sp_Validate")
                });

            var result = await orchestrator.ExecuteAsync(plan);

            Assert.True(result.Success);
            Assert.Equal(2, result.TableCopyResults.Count);
            Assert.Equal(2, result.StoredProcedureResults.Count);
            tm.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_LogsErrorOnFailure()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(false, "Source", "Dest", 0, 100, "Error"));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var plan = new EtlExecutionPlan(
                new List<(string, string, TableCopyOptions)> { ("Source", "Dest", new TableCopyOptions()) },
                new List<StoredProcedureDefinition>());

            await orchestrator.ExecuteAsync(plan);

            logger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_RollbackFailure_IsHandledGracefully()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(false, "Source", "Dest", 0, 100, "Failed"));

            tm.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Rollback failed"));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var plan = new EtlExecutionPlan(
                new List<(string, string, TableCopyOptions)> { ("Source", "Dest", new TableCopyOptions()) },
                new List<StoredProcedureDefinition>());

            var result = await orchestrator.ExecuteAsync(plan);

            Assert.False(result.Success);
            logger.Verify(x => x.LogError(
                It.Is<string>(s => s.Contains("Rollback failed")),
                It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_PassesTokenDownstream()
        {
            var tm = new Mock<ITransactionManager>();
            var tableCopy = new Mock<ITableCopyService>();
            var sp = new Mock<IStoredProcedureService>();
            var logger = new Mock<IEtlLogger>();

            tableCopy.Setup(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TableCopyResult(true, "Source", "Dest", 100, 1000));

            var orchestrator = new EtlOrchestrator(tm.Object, tableCopy.Object, sp.Object, logger.Object);

            var cts = new CancellationTokenSource();
            var plan = new EtlExecutionPlan(
                new List<(string, string, TableCopyOptions)> { ("Source", "Dest", new TableCopyOptions()) },
                new List<StoredProcedureDefinition>());

            var result = await orchestrator.ExecuteAsync(plan, cancellationToken: cts.Token);

            Assert.True(result.Success);
            tableCopy.Verify(x => x.CopyTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TableCopyOptions>(),
                cts.Token), Times.Once);
        }
    }
}
