using System.Data;
using Dapper;
using Dapper.ETL.Library.Implementation;

namespace SQLLite.Integration.Tests;

/// <summary>
/// Integration tests for TransactionManager using a real SQLite database.
/// </summary>
public class TransactionIntegrationTests : IAsyncLifetime {
    private readonly SqliteFixture _fixture;

    public TransactionIntegrationTests() {
        _fixture = new SqliteFixture();
    }

    public async Task InitializeAsync() {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task BeginTransactionAsync_OpensConnectionAndStartsTransaction() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);

        // Act
        await transactionManager.BeginTransactionAsync();

        // Assert
        Assert.NotNull(transactionManager.CurrentTransaction);
        Assert.Equal(ConnectionState.Open, _fixture.Connection.State);
    }

    [Fact]
    public async Task CommitAsync_CommitsTransaction() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);
        await transactionManager.BeginTransactionAsync();

        // Insert data within transaction
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = @"
                    INSERT INTO SourceTable (Id, Name, Value, Description)
                    VALUES (1, 'TestItem', 100, 'TestDesc')";
            cmd.Transaction = transactionManager.CurrentTransaction;
            cmd.ExecuteNonQuery();
        }

        // Act
        await transactionManager.CommitAsync();

        // Assert
        var result = _fixture.Connection.QueryFirstOrDefault<dynamic>(
            "SELECT * FROM SourceTable WHERE Id = 1");
        Assert.NotNull(result);
        Assert.Equal("TestItem", (string)result!.Name);
    }

    [Fact]
    public async Task RollbackAsync_RollsBackTransaction() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);
        await transactionManager.BeginTransactionAsync();

        // Insert data within transaction
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = @"
                    INSERT INTO SourceTable (Id, Name, Value, Description)
                    VALUES (2, 'RollbackItem', 200, 'WillRollback')";
            cmd.Transaction = transactionManager.CurrentTransaction;
            cmd.ExecuteNonQuery();
        }

        // Act
        await transactionManager.RollbackAsync();

        // Assert
        var result = _fixture.Connection.QueryFirstOrDefault<dynamic>(
            "SELECT * FROM SourceTable WHERE Id = 2");
        Assert.Null(result);
    }

    [Fact]
    public async Task MultipleTransactions_CanBeCreatedSequentially() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);

        // Act - First transaction
        await transactionManager.BeginTransactionAsync();
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = @"
                    INSERT INTO SourceTable (Id, Name, Value, Description)
                    VALUES (3, 'Item3', 300, 'Desc3')";
            cmd.Transaction = transactionManager.CurrentTransaction;
            cmd.ExecuteNonQuery();
        }
        await transactionManager.CommitAsync();

        // Act - Second transaction
        await transactionManager.BeginTransactionAsync();
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = @"
                    INSERT INTO SourceTable (Id, Name, Value, Description)
                    VALUES (4, 'Item4', 400, 'Desc4')";
            cmd.Transaction = transactionManager.CurrentTransaction;
            cmd.ExecuteNonQuery();
        }
        await transactionManager.CommitAsync();

        // Assert
        var count = _fixture.Connection.QuerySingleOrDefault<int>(
            "SELECT COUNT(*) FROM SourceTable");
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task DisposeAsync_ClosesConnectionProperly() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);
        await transactionManager.BeginTransactionAsync();

        // Act
        await transactionManager.DisposeAsync();

        // Assert - Connection should be closed
        Assert.Equal(ConnectionState.Closed, _fixture.Connection.State);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithIsolationLevel_SetsCorrectLevel() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);

        // Act
        await transactionManager.BeginTransactionAsync(IsolationLevel.Serializable);

        // Assert
        Assert.NotNull(transactionManager.CurrentTransaction);
        Assert.Equal(IsolationLevel.Serializable, transactionManager.CurrentTransaction.IsolationLevel);
    }

    [Fact]
    public async Task RollbackAsync_WhenNoTransaction_DoesNotThrow() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);

        // Act & Assert - Should not throw
        await transactionManager.RollbackAsync();
    }

    [Fact]
    public async Task CommitAsync_WhenNoTransaction_DoesNotThrow() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);

        // Act & Assert - Should not throw
        await transactionManager.CommitAsync();
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenDisposed_ThrowsObjectDisposedException() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);
        await transactionManager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await transactionManager.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitAsync_WhenDisposed_ThrowsObjectDisposedException() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);
        await transactionManager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await transactionManager.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_WhenDisposed_ThrowsObjectDisposedException() {
        // Arrange
        var transactionManager = new TransactionManager(_fixture.Connection);
        await transactionManager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await transactionManager.RollbackAsync());
    }
}