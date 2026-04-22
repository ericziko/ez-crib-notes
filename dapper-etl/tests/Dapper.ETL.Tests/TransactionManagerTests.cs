using System;
using System.Data;
using System.Threading.Tasks;
using Dapper.ETL.Library.Implementation;
using Moq;
using Xunit;

namespace Dapper.ETL.Tests;

/// <summary>
/// Tests for TransactionManager using Verify.NET for snapshot verification
/// </summary>
public class TransactionManagerTests {
    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TransactionManager(null!));
    }

    [Fact]
    public void Constructor_WithValidConnection_InitializesSuccessfully() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();

        // Act
        var manager = new TransactionManager(mockConnection.Object);

        // Assert
        Assert.NotNull(manager);
        Assert.Equal(mockConnection.Object, manager.Connection);
        Assert.Null(manager.CurrentTransaction);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithClosedConnection_OpensConnection() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Closed);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.BeginTransactionAsync();

        // Assert
        mockConnection.Verify(c => c.Open(), Times.Once);
        mockConnection.Verify(c => c.BeginTransaction(IsolationLevel.ReadCommitted), Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithOpenConnection_DoesNotOpenAgain() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.BeginTransactionAsync();

        // Assert
        mockConnection.Verify(c => c.Open(), Times.Never);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithCustomIsolationLevel_UsesSpecifiedLevel() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(IsolationLevel.Serializable))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.BeginTransactionAsync(IsolationLevel.Serializable);

        // Assert
        mockConnection.Verify(c => c.BeginTransaction(IsolationLevel.Serializable), Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_AfterDispose_ThrowsObjectDisposedException() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var manager = new TransactionManager(mockConnection.Object);
        await manager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitAsync_WithActiveTransaction_CommitsAndDisposesTransaction() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);
        await manager.BeginTransactionAsync();

        // Act
        await manager.CommitAsync();

        // Assert
        mockTransaction.Verify(t => t.Commit(), Times.Once);
        mockTransaction.Verify(t => t.Dispose(), Times.Once);
        Assert.Null(manager.CurrentTransaction);
    }

    [Fact]
    public async Task CommitAsync_WithoutActiveTransaction_DoesNotThrow() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var manager = new TransactionManager(mockConnection.Object);

        // Act & Assert - should not throw
        await manager.CommitAsync();
        Assert.Null(manager.CurrentTransaction);
    }

    [Fact]
    public async Task CommitAsync_AfterDispose_ThrowsObjectDisposedException() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var manager = new TransactionManager(mockConnection.Object);
        await manager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_WithActiveTransaction_RollsBackAndDisposesTransaction() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);
        await manager.BeginTransactionAsync();

        // Act
        await manager.RollbackAsync();

        // Assert
        mockTransaction.Verify(t => t.Rollback(), Times.Once);
        mockTransaction.Verify(t => t.Dispose(), Times.Once);
        Assert.Null(manager.CurrentTransaction);
    }

    [Fact]
    public async Task RollbackAsync_WithoutActiveTransaction_DoesNotThrow() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var manager = new TransactionManager(mockConnection.Object);

        // Act & Assert - should not throw
        await manager.RollbackAsync();
        Assert.Null(manager.CurrentTransaction);
    }

    [Fact]
    public async Task RollbackAsync_AfterDispose_ThrowsObjectDisposedException() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var manager = new TransactionManager(mockConnection.Object);
        await manager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RollbackAsync());
    }

    [Fact]
    public async Task DisposeAsync_WithActiveTransaction_DisposesTransactionAndConnection() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);
        await manager.BeginTransactionAsync();

        // Act
        await manager.DisposeAsync();

        // Assert
        mockTransaction.Verify(t => t.Dispose(), Times.Once);
        mockConnection.Verify(c => c.Close(), Times.Once);
        mockConnection.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_IsIdempotent() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.DisposeAsync();
        await manager.DisposeAsync();

        // Assert - should not throw and only close once
        mockConnection.Verify(c => c.Close(), Times.AtMostOnce);
    }

    [Fact]
    public async Task DisposeAsync_WithClosedConnection_StillDisposesConnection() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Closed);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.DisposeAsync();

        // Assert
        mockConnection.Verify(c => c.Dispose(), Times.Once);
        mockConnection.Verify(c => c.Close(), Times.Never);
    }

    [Fact]
    public async Task CompleteLifecycle_BeginCommit_SuccessfullyCompletesTransaction() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.BeginTransactionAsync();
        Assert.NotNull(manager.CurrentTransaction);

        await manager.CommitAsync();
        Assert.Null(manager.CurrentTransaction);

        await manager.DisposeAsync();

        // Assert
        mockConnection.Verify(c => c.BeginTransaction(It.IsAny<IsolationLevel>()), Times.Once);
        mockTransaction.Verify(t => t.Commit(), Times.Once);
    }

    [Fact]
    public async Task CompleteLifecycle_BeginRollback_SuccessfullyRollsBackTransaction() {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockTransaction = new Mock<IDbTransaction>();
        mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        mockConnection.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var manager = new TransactionManager(mockConnection.Object);

        // Act
        await manager.BeginTransactionAsync();
        Assert.NotNull(manager.CurrentTransaction);

        await manager.RollbackAsync();
        Assert.Null(manager.CurrentTransaction);

        await manager.DisposeAsync();

        // Assert
        mockConnection.Verify(c => c.BeginTransaction(It.IsAny<IsolationLevel>()), Times.Once);
        mockTransaction.Verify(t => t.Rollback(), Times.Once);
    }
}