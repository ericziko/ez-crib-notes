namespace Dapper.ETL.Library.Implementation
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper.ETL.Library.Interfaces;

    /// <summary>
    /// Implementation of transaction management wrapping an IDbConnection and IDbTransaction.
    /// </summary>
    public class TransactionManager : ITransactionManager
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionManager"/> class.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        public TransactionManager(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Gets the underlying database connection.
        /// </summary>
        public IDbConnection Connection => _connection;

        /// <summary>
        /// Gets the current transaction.
        /// </summary>
        public IDbTransaction? CurrentTransaction => _transaction;

        /// <summary>
        /// Begins a new transaction asynchronously.
        /// </summary>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionManager));
            }

            if (_connection.State != ConnectionState.Open)
            {
                await Task.Run(() => _connection.Open(), cancellationToken).ConfigureAwait(false);
            }

            _transaction = _connection.BeginTransaction(isolationLevel);
        }

        /// <summary>
        /// Commits the current transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionManager));
            }

            if (_transaction != null)
            {
                await Task.Run(() => _transaction.Commit(), cancellationToken).ConfigureAwait(false);
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionManager));
            }

            if (_transaction != null)
            {
                await Task.Run(() => _transaction.Rollback(), cancellationToken).ConfigureAwait(false);
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Disposes the transaction manager.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await Task.Run(() => _connection.Close()).ConfigureAwait(false);
                }
                _connection.Dispose();
            }

            _disposed = true;
        }
    }
}
