namespace Dapper.ETL.Library.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper.ETL.Library.Models;

    /// <summary>
    /// Interface for orchestrating ETL operations.
    /// </summary>
    public interface IEtlOrchestrator
    {
        /// <summary>
        /// Executes an ETL execution plan asynchronously.
        /// </summary>
        /// <param name="plan">The ETL execution plan to execute.</param>
        /// <param name="shouldRollback">Whether to rollback the transaction on failure.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the ETL execution.</returns>
        Task<EtlExecutionResult> ExecuteAsync(
            EtlExecutionPlan plan,
            bool shouldRollback = true,
            CancellationToken cancellationToken = default);
    }
}
