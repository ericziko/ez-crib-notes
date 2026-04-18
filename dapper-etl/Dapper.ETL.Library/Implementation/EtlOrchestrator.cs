namespace Dapper.ETL.Library.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper.ETL.Library.Interfaces;
    using Dapper.ETL.Library.Models;

    /// <summary>
    /// Service for orchestrating complete ETL workflows.
    /// </summary>
    public class EtlOrchestrator : IEtlOrchestrator
    {
        private readonly ITransactionManager _transactionManager;
        private readonly ITableCopyService _tableCopyService;
        private readonly IStoredProcedureService _storedProcedureService;
        private readonly IEtlLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtlOrchestrator"/> class.
        /// </summary>
        /// <param name="transactionManager">The transaction manager.</param>
        /// <param name="tableCopyService">The table copy service.</param>
        /// <param name="storedProcedureService">The stored procedure service.</param>
        /// <param name="logger">The ETL logger.</param>
        public EtlOrchestrator(
            ITransactionManager transactionManager,
            ITableCopyService tableCopyService,
            IStoredProcedureService storedProcedureService,
            IEtlLogger logger)
        {
            _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
            _tableCopyService = tableCopyService ?? throw new ArgumentNullException(nameof(tableCopyService));
            _storedProcedureService = storedProcedureService ?? throw new ArgumentNullException(nameof(storedProcedureService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes an ETL execution plan asynchronously.
        /// </summary>
        /// <param name="plan">The ETL execution plan to execute.</param>
        /// <param name="shouldRollback">Whether to rollback the transaction on failure.</param>
        /// <param name="transactionMode">Controls whether all copies share one transaction (Atomic) or each gets its own (Partial).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the ETL execution.</returns>
        public async Task<EtlExecutionResult> ExecuteAsync(
            EtlExecutionPlan plan,
            bool shouldRollback = true,
            EtlTransactionMode transactionMode = EtlTransactionMode.Atomic,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(plan);

            if (transactionMode == EtlTransactionMode.Partial)
            {
                return await ExecutePartialAsync(plan, cancellationToken);
            }

            return await ExecuteAtomicAsync(plan, shouldRollback, cancellationToken);
        }

        private async Task<EtlExecutionResult> ExecuteAtomicAsync(
            EtlExecutionPlan plan,
            bool shouldRollback,
            CancellationToken cancellationToken)
        {
            var tableCopyResults = new List<TableCopyResult>();
            var storedProcedureResults = new List<StoredProcedureResult>();

            try
            {
                // Begin transaction
                await _transactionManager.BeginTransactionAsync(cancellationToken: cancellationToken);

                // Execute table copies
                foreach (var (sourceTable, destTable, options) in plan.TableCopies)
                {
                    var result = await _tableCopyService.CopyTableAsync(
                        sourceTable,
                        destTable,
                        options,
                        cancellationToken);

                    tableCopyResults.Add(result);

                    if (!result.Success)
                    {
                        throw new InvalidOperationException($"Table copy failed: {result.ErrorMessage}");
                    }
                }

                // Execute stored procedures
                foreach (var procedure in plan.StoredProcedures)
                {
                    var result = await _storedProcedureService.ExecuteAsync(procedure, cancellationToken);
                    storedProcedureResults.Add(result);

                    if (!result.Success)
                    {
                        throw new InvalidOperationException($"Stored procedure execution failed: {result.ErrorMessage}");
                    }
                }

                // Commit transaction
                await _transactionManager.CommitAsync(cancellationToken);

                return new EtlExecutionResult(
                    true,
                    tableCopyResults,
                    storedProcedureResults);
            }
            catch (Exception ex)
            {
                _logger.LogError("ETL execution failed", ex);

                if (shouldRollback)
                {
                    try
                    {
                        await _transactionManager.RollbackAsync(cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError("Rollback failed", rollbackEx);
                    }
                }

                return new EtlExecutionResult(
                    false,
                    tableCopyResults,
                    storedProcedureResults,
                    ex.Message);
            }
        }

        private async Task<EtlExecutionResult> ExecutePartialAsync(
            EtlExecutionPlan plan,
            CancellationToken cancellationToken)
        {
            var tableCopyResults = new List<TableCopyResult>();
            var storedProcedureResults = new List<StoredProcedureResult>();
            string? overallError = null;

            // Each table copy in its own transaction; continue on error
            foreach (var (sourceTable, destTable, options) in plan.TableCopies)
            {
                try
                {
                    await _transactionManager.BeginTransactionAsync(cancellationToken: cancellationToken);

                    var result = await _tableCopyService.CopyTableAsync(
                        sourceTable,
                        destTable,
                        options,
                        cancellationToken);

                    tableCopyResults.Add(result);

                    if (!result.Success)
                    {
                        _logger.LogError($"Table copy failed for {sourceTable}: {result.ErrorMessage}", new InvalidOperationException(result.ErrorMessage));
                        await _transactionManager.RollbackAsync(cancellationToken);
                        overallError ??= result.ErrorMessage;
                    }
                    else
                    {
                        await _transactionManager.CommitAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Table copy exception for {sourceTable}", ex);
                    overallError = overallError ?? ex.Message;

                    try
                    {
                        await _transactionManager.RollbackAsync(cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError("Rollback failed", rollbackEx);
                    }

                    tableCopyResults.Add(new TableCopyResult(false, sourceTable, destTable, 0, 0, ex.Message));
                }
            }

            // Stored procedures run after all table copies, in a single transaction
            if (plan.StoredProcedures.Count > 0)
            {
                try
                {
                    await _transactionManager.BeginTransactionAsync(cancellationToken: cancellationToken);

                    foreach (var procedure in plan.StoredProcedures)
                    {
                        var result = await _storedProcedureService.ExecuteAsync(procedure, cancellationToken);
                        storedProcedureResults.Add(result);

                        if (!result.Success)
                        {
                            throw new InvalidOperationException($"Stored procedure execution failed: {result.ErrorMessage}");
                        }
                    }

                    await _transactionManager.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Stored procedure execution failed in partial mode", ex);
                    overallError = overallError ?? ex.Message;

                    try
                    {
                        await _transactionManager.RollbackAsync(cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError("Rollback failed", rollbackEx);
                    }
                }
            }

            bool overallSuccess = overallError == null;
            return new EtlExecutionResult(
                overallSuccess,
                tableCopyResults,
                storedProcedureResults,
                overallError);
        }
    }
}
