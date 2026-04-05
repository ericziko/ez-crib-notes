namespace Dapper.ETL.Library.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for processing data in batches.
    /// </summary>
    public interface IBatchProcessor
    {
        /// <summary>
        /// Processes items in batches asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of items to process.</typeparam>
        /// <param name="items">The items to process.</param>
        /// <param name="batchSize">The size of each batch.</param>
        /// <param name="processBatch">The async function to execute for each batch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ProcessInBatchesAsync<T>(
            IEnumerable<T> items,
            int batchSize,
            Func<List<T>, int, CancellationToken, Task> processBatch,
            CancellationToken cancellationToken = default);
    }
}
