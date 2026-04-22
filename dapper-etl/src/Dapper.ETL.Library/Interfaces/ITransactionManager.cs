using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.ETL.Library.Interfaces;

/// <summary>
/// Interface for managing database transactions.
/// </summary>
public interface ITransactionManager : IAsyncDisposable {

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the current transaction.
    /// </summary>
    IDbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Begins a new transaction asynchronously.
    /// </summary>
    /// <param name="isolationLevel">The isolation level of the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}