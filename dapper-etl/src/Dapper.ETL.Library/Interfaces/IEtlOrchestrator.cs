using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Models;

namespace Dapper.ETL.Library.Interfaces;

/// <summary>
///     Interface for orchestrating ETL operations.
/// </summary>
public interface IEtlOrchestrator {
    /// <summary>
    ///     Executes an ETL execution plan asynchronously.
    /// </summary>
    /// <param name="plan">The ETL execution plan to execute.</param>
    /// <param name="shouldRollback">Whether to rollback the transaction on failure.</param>
    /// <param name="transactionMode">
    ///     Controls whether all copies share one transaction (Atomic) or each gets its own
    ///     (Partial).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the ETL execution.</returns>
    Task<EtlExecutionResult> ExecuteAsync(
        EtlExecutionPlan plan,
        bool shouldRollback = true,
        EtlTransactionMode transactionMode = EtlTransactionMode.Atomic,
        CancellationToken cancellationToken = default);
}