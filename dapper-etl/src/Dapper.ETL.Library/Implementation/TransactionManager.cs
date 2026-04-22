using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Interfaces;

namespace Dapper.ETL.Library.Implementation;

/// <summary>
/// Implementation of transaction management wrapping an IDbConnection and IDbTransaction.
/// </summary>
public class TransactionManager : ITransactionManager {
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionManager" /> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    public TransactionManager(IDbConnection connection) {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    public IDbConnection Connection { get; }

    /// <summary>
    /// Gets the current transaction.
    /// </summary>
    public IDbTransaction? CurrentTransaction { get; private set; }

    /// <summary>
    /// Begins a new transaction asynchronously.
    /// </summary>
    /// <param name="isolationLevel">The isolation level of the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(TransactionManager));
        }

        if (Connection.State != ConnectionState.Open) {
            await Task.Run(() => Connection.Open(), cancellationToken).ConfigureAwait(false);
        }

        CurrentTransaction = Connection.BeginTransaction(isolationLevel);
    }

    /// <summary>
    /// Commits the current transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CommitAsync(CancellationToken cancellationToken = default) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(TransactionManager));
        }

        if (CurrentTransaction != null) {
            await Task.Run(() => CurrentTransaction.Commit(), cancellationToken).ConfigureAwait(false);
            CurrentTransaction.Dispose();
            CurrentTransaction = null;
        }
    }

    /// <summary>
    /// Rolls back the current transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RollbackAsync(CancellationToken cancellationToken = default) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(TransactionManager));
        }

        if (CurrentTransaction != null) {
            await Task.Run(() => CurrentTransaction.Rollback(), cancellationToken).ConfigureAwait(false);
            CurrentTransaction.Dispose();
            CurrentTransaction = null;
        }
    }

    /// <summary>
    /// Disposes the transaction manager.
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (_disposed) {
            return;
        }

        if (CurrentTransaction != null) {
            CurrentTransaction.Dispose();
            CurrentTransaction = null;
        }

        if (Connection != null) {
            if (Connection.State == ConnectionState.Open) {
                await Task.Run(() => Connection.Close()).ConfigureAwait(false);
            }
            Connection.Dispose();
        }

        _disposed = true;
    }
}