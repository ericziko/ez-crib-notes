namespace Dapper.ETL.Library.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using Dapper.ETL.Library.Interfaces;
    using Dapper.ETL.Library.Models;

    /// <summary>
    /// Service for executing stored procedures using Dapper.
    /// </summary>
    public class StoredProcedureService : IStoredProcedureService
    {
        private readonly ITransactionManager _transactionManager;
        private readonly IEtlLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoredProcedureService"/> class.
        /// </summary>
        /// <param name="transactionManager">The transaction manager.</param>
        /// <param name="logger">The ETL logger.</param>
        public StoredProcedureService(
            ITransactionManager transactionManager,
            IEtlLogger logger)
        {
            _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a stored procedure asynchronously.
        /// </summary>
        /// <param name="procedureDefinition">The definition of the stored procedure to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the stored procedure execution.</returns>
        public async Task<StoredProcedureResult> ExecuteAsync(
            StoredProcedureDefinition procedureDefinition,
            CancellationToken cancellationToken = default)
        {
            if (procedureDefinition == null)
            {
                throw new ArgumentNullException(nameof(procedureDefinition));
            }

            var connection = _transactionManager.Connection;

            try
            {
                var parameters = new DynamicParameters();

                if (procedureDefinition.Parameters != null)
                {
                    foreach (var param in procedureDefinition.Parameters)
                    {
                        parameters.Add(param.Key, param.Value);
                    }
                }

                var rowsAffected = await connection.ExecuteAsync(
                    procedureDefinition.ProcedureName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: _transactionManager.CurrentTransaction);

                _logger.LogStoredProcedureExecuted(procedureDefinition.ProcedureName, rowsAffected);

                return new StoredProcedureResult(true, procedureDefinition.ProcedureName, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing stored procedure {procedureDefinition.ProcedureName}", ex);
                return new StoredProcedureResult(false, procedureDefinition.ProcedureName, 0, ex.Message);
            }
        }
    }
}
