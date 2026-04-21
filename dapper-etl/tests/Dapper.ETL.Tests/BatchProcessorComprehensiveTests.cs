namespace Dapper.ETL.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library.Implementation;
using Xunit;

/// <summary>
/// Comprehensive tests for BatchProcessor covering all code paths
/// </summary>
public class BatchProcessorComprehensiveTests
{
    private readonly BatchProcessor _processor = new();

    [Fact]
    public async Task ProcessInBatchesAsync_WithNullItems_ThrowsArgumentNullException()
    {
        var processor = new BatchProcessor();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            processor.ProcessInBatchesAsync<int>(
                null!,
                10,
                async (_, _, _) => await Task.CompletedTask));
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithZeroBatchSize_ThrowsArgumentException()
    {
        var items = new[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _processor.ProcessInBatchesAsync(
                items,
                0,
                async (_, _, _) => await Task.CompletedTask));
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithNegativeBatchSize_ThrowsArgumentException()
    {
        var items = new[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _processor.ProcessInBatchesAsync(
                items,
                -1,
                async (_, _, _) => await Task.CompletedTask));
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithNullProcessBatch_ThrowsArgumentNullException()
    {
        var items = new[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _processor.ProcessInBatchesAsync(
                items,
                10,
                null!));
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithEmptyItems_DoesNotProcessAnyBatches()
    {
        var items = new List<int>();
        var processedCount = 0;

        await _processor.ProcessInBatchesAsync(
            items,
            10,
            async (_,_,_) =>
            {
                processedCount++;
                await Task.CompletedTask;
            });

        Assert.Equal(0, processedCount);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithSingleItem_ProcessesAsBatch()
    {
        var items = new[] { 42 };
        var batchNumbers = new List<int>();

        await _processor.ProcessInBatchesAsync(
            items,
            10,
            async (batch, batchNum, ct) =>
            {
                batchNumbers.Add(batchNum);
                await Task.CompletedTask;
            });

        Assert.Single(batchNumbers);
        Assert.Equal(1, batchNumbers[0]);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_BatchNumbersStartAt1()
    {
        var items = Enumerable.Range(1, 35).ToList();
        var batchNumbers = new List<int>();

        await _processor.ProcessInBatchesAsync(
            items,
            10,
            async (batch, batchNum, ct) =>
            {
                batchNumbers.Add(batchNum);
                await Task.CompletedTask;
            });

        Assert.Equal(new[] { 1, 2, 3, 4 }, batchNumbers);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithProcessBatchException_PropagatesException()
    {
        var items = new[] { 1, 2, 3 };
        var exception = new InvalidOperationException("Batch processing failed");

        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.ProcessInBatchesAsync(
                items,
                2, (batch, batchNum, ct) =>
                {
                    try {
                        throw exception;
                    }
                    catch (Exception exception1) {
                        return Task.FromException(exception1);
                    }
                }));

        Assert.Equal("Batch processing failed", thrownException.Message);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_CancellationTokenPassedToBatch()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var receivedTokens = new List<CancellationToken>();

        var cts = new CancellationTokenSource();
        await _processor.ProcessInBatchesAsync(
            items,
            2,
            async (batch, batchNum, ct) =>
            {
                receivedTokens.Add(ct);
                await Task.CompletedTask;
            },
            cts.Token);

        Assert.Equal(3, receivedTokens.Count); // 3 batches: [2 items], [2 items], [1 item]
        Assert.All(receivedTokens, token => Assert.Equal(cts.Token, token));
    }

    [Fact]
    public async Task ProcessInBatchesAsync_RemainingItemsProcessedInFinalBatch()
    {
        var items = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var batchSizes = new List<int>();

        await _processor.ProcessInBatchesAsync(
            items,
            4,
            async (batch, batchNum, ct) =>
            {
                batchSizes.Add(batch.Count);
                await Task.CompletedTask;
            });

        Assert.Equal(new[] { 4, 4, 1 }, batchSizes);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_BatchContainsCorrectItems()
    {
        var items = new[] { 10, 20, 30, 40, 50 };
        var allBatchItems = new List<List<int>>();

        await _processor.ProcessInBatchesAsync(
            items,
            2,
            async (batch, batchNum, ct) =>
            {
                allBatchItems.Add(new List<int>(batch));
                await Task.CompletedTask;
            });

        Assert.Equal(3, allBatchItems.Count);
        Assert.Equal(new[] { 10, 20 }, allBatchItems[0]);
        Assert.Equal(new[] { 30, 40 }, allBatchItems[1]);
        Assert.Equal(new[] { 50 }, allBatchItems[2]);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_LargeBatchSize_ProcessesAllInOneBatch()
    {
        var items = Enumerable.Range(1, 100).ToList();
        var batchCount = 0;

        await _processor.ProcessInBatchesAsync(
            items,
            1000,
            async (batch, batchNum, ct) =>
            {
                batchCount++;
                Assert.Equal(100, batch.Count);
                await Task.CompletedTask;
            });

        Assert.Equal(1, batchCount);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithStringItems_ProcessesCorrectly()
    {
        var items = new[] { "a", "b", "c", "d", "e", "f" };
        var batchCount = 0;

        await _processor.ProcessInBatchesAsync(
            items,
            2,
            async (batch, batchNum, ct) =>
            {
                batchCount++;
                Assert.Equal(2, batch.Count);
                await Task.CompletedTask;
            });

        Assert.Equal(3, batchCount);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_WithComplexObjects_ProcessesCorrectly()
    {
        var items = new List<(int Id, string Name)>
        {
            (1, "A"),
            (2, "B"),
            (3, "C"),
            (4, "D")
        };
        var processedIds = new List<int>();

        await _processor.ProcessInBatchesAsync(
            items,
            2,
            async (batch, batchNum, ct) =>
            {
                processedIds.AddRange(batch.Select(x => x.Id));
                await Task.CompletedTask;
            });

        Assert.Equal(new[] { 1, 2, 3, 4 }, processedIds);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_OperationIsAsync()
    {
        var items = new[] { 1, 2, 3 };
        var processingOrder = new List<int>();

        await _processor.ProcessInBatchesAsync(
            items,
            2,
            async (batch, batchNum, ct) =>
            {
                processingOrder.Add(batchNum);
                await Task.Delay(10);
            });

        Assert.Equal(2, processingOrder.Count);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_BatchSizeEqualToItemCount()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var batchCount = 0;

        await _processor.ProcessInBatchesAsync(
            items,
            5,
            async (batch, batchNum, ct) =>
            {
                batchCount++;
                Assert.Equal(5, batch.Count);
                await Task.CompletedTask;
            });

        Assert.Equal(1, batchCount);
    }

    [Fact]
    public async Task ProcessInBatchesAsync_BatchSizeOfOne_CreatesMultipleBatches()
    {
        var items = new[] { 1, 2, 3 };
        var batchCount = 0;

        await _processor.ProcessInBatchesAsync(
            items,
            1,
            async (batch, batchNum, ct) =>
            {
                batchCount++;
                Assert.Single(batch);
                await Task.CompletedTask;
            });

        Assert.Equal(3, batchCount);
    }
}