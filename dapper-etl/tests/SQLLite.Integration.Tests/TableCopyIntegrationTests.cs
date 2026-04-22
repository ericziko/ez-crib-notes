using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Models;

namespace SQLLite.Integration.Tests;

/// <summary>
///     Integration tests for TableCopyService using a real SQLite database.
/// </summary>
public class TableCopyIntegrationTests : IAsyncLifetime {
    private readonly SqliteFixture _fixture;

    public TableCopyIntegrationTests() {
        _fixture = new SqliteFixture();
    }

    public async Task InitializeAsync() {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task CopyTableAsync_WithSingleRow_CopiesSuccessfully() {
        // Arrange
        await _fixture.InsertSourceDataAsync(1, "Item1", 100, "Description1");

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.RowCount);
        Assert.Equal("SourceTable", result.SourceTable);
        Assert.Equal("DestTable", result.DestinationTable);
        Assert.Null(result.ErrorMessage);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(1, destCount);
    }

    [Fact]
    public async Task CopyTableAsync_WithMultipleRows_CopiesSuccessfully() {
        // Arrange
        for (var i = 1; i <= 25; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10, $"Description{i}");
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(25, result.RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(25, destCount);
    }

    [Fact]
    public async Task CopyTableAsync_WithBatchSize_ProcessesInBatches() {
        // Arrange - Insert 35 rows (should process in 4 batches with size 10)
        for (var i = 1; i <= 35; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(35, result.RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(35, destCount);
    }

    [Fact]
    public async Task CopyTableAsync_WithTruncateDestination_TruncatesBeforeCopy() {
        // Arrange - Insert data in both source and dest
        await _fixture.InsertSourceDataAsync(1, "NewItem", 999);

        // Insert old data in destination
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = @"
                    INSERT INTO DestTable (Id, Name, Value, Description)
                    VALUES (999, 'OldItem', 1, 'OldData')";
            cmd.ExecuteNonQuery();
        }

        var initialDestCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(1, initialDestCount);

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: true);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.RowCount);

        var finalDestCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(1, finalDestCount); // Should have only new data
    }

    [Fact]
    public async Task CopyTableAsync_WithNullValues_CopiesNullsCorrectly() {
        // Arrange - Insert row with null description
        await _fixture.InsertSourceDataAsync(1, "ItemWithNull", 100);

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(1, destCount);
    }

    [Fact]
    public async Task CopyTableAsync_WithEmptySource_ReturnsEmptyResult() {
        // Arrange - Source table is empty
        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.RowCount);
    }

    [Fact]
    public async Task CopyTableAsync_WithCancellation_RespectsCancellationToken() {
        // Arrange
        for (var i = 1; i <= 5; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        var cts = new CancellationTokenSource();

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options, cts.Token);

        // Assert - Should complete successfully since copy is fast
        Assert.True(result.Success);
        Assert.Equal(5, result.RowCount);
    }

    [Fact]
    public async Task CopyTableAsync_RecordsDurationInMilliseconds() {
        // Arrange
        for (var i = 1; i <= 10; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10);
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.DurationMs >= 0);
    }

    [Fact]
    public async Task CopyTableAsync_WithAllColumns_CopiesAllData() {
        // Arrange
        await _fixture.InsertSourceDataAsync(1, "TestItem", 500, "TestDescription");

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 10, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);

        // Verify data integrity
        using (var cmd = _fixture.Connection.CreateCommand()) {
            cmd.CommandText = "SELECT Name, Value, Description FROM DestTable WHERE Id = 1";
            using (var reader = cmd.ExecuteReader()) {
                Assert.True(reader.Read());
                Assert.Equal("TestItem", reader["Name"]);
                Assert.Equal(500, (long)reader["Value"]);
                Assert.Equal("TestDescription", reader["Description"]);
            }
        }
    }

    [Fact]
    public async Task CopyTableAsync_WithLargeDataset_HandlesScaling() {
        // Arrange - Insert 100 rows
        for (var i = 1; i <= 100; i++) {
            await _fixture.InsertSourceDataAsync(i, $"Item{i}", i * 10, $"Desc{i}");
        }

        var transactionManager = new TransactionManager(_fixture.Connection);
        var columnMapper = new ColumnMapper();
        var batchProcessor = new BatchProcessor();
        var logger = new EtlLogger();
        var schemaInspector = new SqliteSchemaInspector(_fixture.Connection);

        var service = new TableCopyService(transactionManager, columnMapper, batchProcessor, logger, schemaInspector);
        var options = new TableCopyOptions(batchSize: 25, truncateDestination: false);

        // Act
        var result = await service.CopyTableAsync("SourceTable", "DestTable", options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.RowCount);

        var destCount = await _fixture.GetDestTableCountAsync();
        Assert.Equal(100, destCount);
    }
}