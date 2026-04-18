namespace Dapper.ETL.Library.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using Dapper.ETL.Library.Interfaces;
    using Dapper.ETL.Library.Models;

    /// <summary>
    /// Service for copying data between tables using Dapper.
    /// </summary>
    public class TableCopyService : ITableCopyService
    {
        private readonly ITransactionManager _transactionManager;
        private readonly IColumnMapper _columnMapper;
        private readonly IBatchProcessor _batchProcessor;
        private readonly IEtlLogger _logger;
        private readonly ISchemaInspector _schemaInspector;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableCopyService"/> class.
        /// </summary>
        /// <param name="transactionManager">The transaction manager.</param>
        /// <param name="columnMapper">The column mapper.</param>
        /// <param name="batchProcessor">The batch processor.</param>
        /// <param name="logger">The ETL logger.</param>
        /// <param name="schemaInspector">The schema inspector for database-agnostic column retrieval.</param>
        public TableCopyService(
            ITransactionManager transactionManager,
            IColumnMapper columnMapper,
            IBatchProcessor batchProcessor,
            IEtlLogger logger,
            ISchemaInspector schemaInspector)
        {
            _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
            _columnMapper = columnMapper ?? throw new ArgumentNullException(nameof(columnMapper));
            _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _schemaInspector = schemaInspector ?? throw new ArgumentNullException(nameof(schemaInspector));
        }

        /// <summary>
        /// Copies data from a source table to a destination table asynchronously.
        /// </summary>
        /// <param name="sourceTable">The name of the source table.</param>
        /// <param name="destinationTable">The name of the destination table.</param>
        /// <param name="options">Options for the table copy operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the table copy operation.</returns>
        public async Task<TableCopyResult> CopyTableAsync(
            string sourceTable,
            string destinationTable,
            TableCopyOptions options,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceTable))
            {
                throw new ArgumentException("Source table name cannot be null or empty.", nameof(sourceTable));
            }

            if (string.IsNullOrWhiteSpace(destinationTable))
            {
                throw new ArgumentException("Destination table name cannot be null or empty.", nameof(destinationTable));
            }

            ArgumentNullException.ThrowIfNull(options);

            var stopwatch = Stopwatch.StartNew();
            var connection = _transactionManager.Connection;

            try
            {
                // Get column information using database-agnostic schema inspector
                var sourceColumns = await _schemaInspector.GetColumnNamesAsync(sourceTable);
                var destColumns = await _schemaInspector.GetColumnNamesAsync(destinationTable);

                if (sourceColumns.Count == 0)
                {
                    return new TableCopyResult(false, sourceTable, destinationTable, 0, stopwatch.ElapsedMilliseconds, "Source table has no columns.");
                }

                // Get column mappings
                var mappings = _columnMapper.GetMapping(
                    sourceColumns,
                    destColumns,
                    options.ColumnMappingOverrides);

                var mappingsList = mappings.ToList();
                if (mappingsList.Count == 0)
                {
                    return new TableCopyResult(false, sourceTable, destinationTable, 0, stopwatch.ElapsedMilliseconds, "No matching columns between source and destination.");
                }

                // Truncate destination if requested
                if (options.TruncateDestination)
                {
                    await connection.ExecuteAsync(
                        $"DELETE FROM \"{destinationTable}\"",
                        transaction: _transactionManager.CurrentTransaction);
                    _logger.LogTableTruncated(destinationTable);
                }

                // Get data from source
                var selectClause = _columnMapper.GetSelectClause(mappingsList);
                var sourceData = (await connection.QueryAsync<dynamic>(
                    $"SELECT {selectClause} FROM \"{sourceTable}\"",
                    transaction: _transactionManager.CurrentTransaction)).ToList();

                _logger.LogTableCopyStarted(sourceTable, sourceData.Count);

                // Insert data in batches
                var insertClause = _columnMapper.GetInsertClause(mappingsList);
                var insertQuery = $"INSERT INTO \"{destinationTable}\" ({insertClause}) VALUES ";

                await _batchProcessor.ProcessInBatchesAsync(
                    sourceData,
                    options.BatchSize,
                    async (batch, batchNumber, ct) =>
                    {
                        var values = new List<string>();
                        for (int i = 0; i < batch.Count; i++)
                        {
                            var columnPlaceholders = string.Join(", ", Enumerable.Range(0, mappingsList.Count).Select(j => $"@p{i}_{j}"));
                            values.Add($"({columnPlaceholders})");
                        }

                        var fullQuery = insertQuery + string.Join(", ", values);
                        var parameters = new DynamicParameters();

                        // Add parameters
                        for (int i = 0; i < batch.Count; i++)
                        {
                            var item = batch[i];
                            var colIndex = 0;
                            foreach (var mapping in mappingsList)
                            {
                                var value = ((IDictionary<string, object?>)item)[mapping.SourceColumn];
                                parameters.Add($"@p{i}_{colIndex}", value);
                                colIndex++;
                            }
                        }

                        await connection.ExecuteAsync(fullQuery, parameters, _transactionManager.CurrentTransaction);
                        _logger.LogBatchProcessed(batchNumber, batch.Count);
                    },
                    cancellationToken);

                stopwatch.Stop();
                _logger.LogTableCopyCompleted(sourceTable, sourceData.Count, stopwatch.ElapsedMilliseconds);

                return new TableCopyResult(true, sourceTable, destinationTable, sourceData.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Error copying table {sourceTable} to {destinationTable}", ex);
                return new TableCopyResult(false, sourceTable, destinationTable, 0, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}
