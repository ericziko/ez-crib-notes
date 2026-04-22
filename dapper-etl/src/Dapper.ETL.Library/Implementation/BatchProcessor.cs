using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Interfaces;

namespace Dapper.ETL.Library.Implementation;

/// <summary>
///     Default implementation of batch processing.
/// </summary>
public class BatchProcessor : IBatchProcessor {
    /// <summary>
    ///     Processes items in batches asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <param name="processBatch">The async function to execute for each batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ProcessInBatchesAsync<T>(
        IEnumerable<T> items,
        int batchSize,
        Func<List<T>, int, CancellationToken, Task> processBatch,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(items);

        if (batchSize <= 0) {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }

        ArgumentNullException.ThrowIfNull(processBatch);

        var batch = new List<T>(batchSize);
        var batchNumber = 0;

        foreach (var item in items) {
            batch.Add(item);

            if (batch.Count == batchSize) {
                batchNumber++;
                await processBatch(batch, batchNumber, cancellationToken).ConfigureAwait(false);
                batch.Clear();
            }
        }

        // Process remaining items
        if (batch.Count > 0) {
            batchNumber++;
            await processBatch(batch, batchNumber, cancellationToken).ConfigureAwait(false);
        }
    }
}