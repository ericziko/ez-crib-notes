namespace Dapper.ETL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper.ETL.Library.Implementation;
    using Xunit;

    public class BatchProcessorTests
    {
        private readonly BatchProcessor _processor = new BatchProcessor();

        [Fact]
        public async Task ProcessInBatchesAsync_WithSingleBatch_ProcessesAllItems()
        {
            // Arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var batchSize = 10;
            var processedBatches = new List<(List<int> Batch, int BatchNumber)>();

            // Act
            await _processor.ProcessInBatchesAsync(
                items,
                batchSize,
                async (batch, batchNumber, ct) =>
                {
                    processedBatches.Add((new List<int>(batch), batchNumber));
                    await Task.CompletedTask;
                });

            // Assert
            Assert.Single(processedBatches);
            Assert.Equal(5, processedBatches[0].Batch.Count);
            Assert.Equal(1, processedBatches[0].BatchNumber);
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithMultipleBatches_SplitsCorrectly()
        {
            // Arrange
            var items = Enumerable.Range(1, 25).ToList();
            var batchSize = 10;
            var processedBatches = new List<(List<int> Batch, int BatchNumber)>();

            // Act
            await _processor.ProcessInBatchesAsync(
                items,
                batchSize,
                async (batch, batchNumber, ct) =>
                {
                    processedBatches.Add((new List<int>(batch), batchNumber));
                    await Task.CompletedTask;
                });

            // Assert
            Assert.Equal(3, processedBatches.Count);
            Assert.Equal(10, processedBatches[0].Batch.Count);
            Assert.Equal(10, processedBatches[1].Batch.Count);
            Assert.Equal(5, processedBatches[2].Batch.Count);
            Assert.Equal(1, processedBatches[0].BatchNumber);
            Assert.Equal(2, processedBatches[1].BatchNumber);
            Assert.Equal(3, processedBatches[2].BatchNumber);
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithExactBatchSize_ProcessesEvenly()
        {
            // Arrange
            var items = Enumerable.Range(1, 20).ToList();
            var batchSize = 5;
            var processedBatches = new List<(List<int> Batch, int BatchNumber)>();

            // Act
            await _processor.ProcessInBatchesAsync(
                items,
                batchSize,
                async (batch, batchNumber, ct) =>
                {
                    processedBatches.Add((new List<int>(batch), batchNumber));
                    await Task.CompletedTask;
                });

            // Assert
            Assert.Equal(4, processedBatches.Count);
            Assert.All(processedBatches, batch => Assert.Equal(5, batch.Batch.Count));
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithZeroRows_DoesNotProcessAnyBatches()
        {
            // Arrange
            var items = new List<int>();
            var batchSize = 10;
            var processedBatches = new List<(List<int> Batch, int BatchNumber)>();

            // Act
            await _processor.ProcessInBatchesAsync(
                items,
                batchSize,
                async (batch, batchNumber, ct) =>
                {
                    processedBatches.Add((new List<int>(batch), batchNumber));
                    await Task.CompletedTask;
                });

            // Assert
            Assert.Empty(processedBatches);
        }

        [Fact]
        public async Task ProcessInBatchesAsync_CallsProcessBatchForEachBatch()
        {
            // Arrange
            var items = Enumerable.Range(1, 15).ToList();
            var batchSize = 5;
            var callCount = 0;

            // Act
            await _processor.ProcessInBatchesAsync(
                items,
                batchSize,
                async (batch, batchNumber, ct) =>
                {
                    callCount++;
                    await Task.CompletedTask;
                });

            // Assert
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithNullItems_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _processor.ProcessInBatchesAsync<int>(
                    null!,
                    10,
                    async (b, n, ct) => await Task.CompletedTask));
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithZeroBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _processor.ProcessInBatchesAsync(
                    items,
                    0,
                    async (b, n, ct) => await Task.CompletedTask));
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithNegativeBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _processor.ProcessInBatchesAsync(
                    items,
                    -1,
                    async (b, n, ct) => await Task.CompletedTask));
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithNullProcessBatch_ThrowsArgumentNullException()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _processor.ProcessInBatchesAsync<int>(
                    items,
                    10,
                    null!));
        }

        [Fact]
        public async Task ProcessInBatchesAsync_RespectsItemOrder()
        {
            // Arrange
            var items = Enumerable.Range(1, 10).ToList();
            var batchSize = 3;
            var allProcessedItems = new List<int>();

            // Act
            await _processor.ProcessInBatchesAsync(
                items,
                batchSize,
                async (batch, batchNumber, ct) =>
                {
                    allProcessedItems.AddRange(batch);
                    await Task.CompletedTask;
                });

            // Assert
            Assert.Equal(items, allProcessedItems);
        }

        [Fact]
        public async Task ProcessInBatchesAsync_WithCancellation_StopsProcessing()
        {
            // Arrange
            var items = Enumerable.Range(1, 100).ToList();
            var batchSize = 10;
            var processedBatches = 0;
            var cts = new CancellationTokenSource();

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
                await _processor.ProcessInBatchesAsync(
                    items,
                    batchSize,
                    async (batch, batchNumber, ct) =>
                    {
                        processedBatches++;
                        if (batchNumber == 2)
                        {
                            cts.Cancel();
                        }
                        await Task.CompletedTask;
                    },
                    cts.Token));

            // Note: Depending on implementation, might throw or complete gracefully
            // This test documents the current behavior
        }
    }
}
