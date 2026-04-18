namespace Dapper.ETL.Tests
{
    using System;
    using System.IO;
    using Dapper.ETL.Library.Implementation;
    using Xunit;

    /// <summary>
    /// Tests for the EtlLogger implementation.
    /// </summary>
    public class EtlLoggerTests
    {
        private readonly EtlLogger _logger = new EtlLogger();

        [Fact]
        public void LogTableCopyStarted_WithValidInputs_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var tableName = "Users";
            var rowCount = 1000;

            // Act
            _logger.LogTableCopyStarted(tableName, rowCount);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("Starting table copy", logOutput);
            Assert.Contains(tableName, logOutput);
            Assert.Contains(rowCount.ToString(), logOutput);
        }

        [Fact]
        public void LogTableCopyCompleted_WithValidInputs_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var tableName = "Users";
            var rowCount = 1000;
            var durationMs = 5000L;

            // Act
            _logger.LogTableCopyCompleted(tableName, rowCount, durationMs);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("Completed table copy", logOutput);
            Assert.Contains(tableName, logOutput);
            Assert.Contains(rowCount.ToString(), logOutput);
            Assert.Contains(durationMs.ToString(), logOutput);
        }

        [Fact]
        public void LogTableTruncated_WithValidTableName_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var tableName = "Users";

            // Act
            _logger.LogTableTruncated(tableName);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("Truncated table", logOutput);
            Assert.Contains(tableName, logOutput);
        }

        [Fact]
        public void LogStoredProcedureExecuted_WithValidInputs_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var procedureName = "sp_UpdateUserStats";
            var rowsAffected = 50;

            // Act
            _logger.LogStoredProcedureExecuted(procedureName, rowsAffected);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("Executed stored procedure", logOutput);
            Assert.Contains(procedureName, logOutput);
            Assert.Contains(rowsAffected.ToString(), logOutput);
        }

        [Fact]
        public void LogBatchProcessed_WithValidInputs_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var batchNumber = 3;
            var rowCount = 5000;

            // Act
            _logger.LogBatchProcessed(batchNumber, rowCount);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("Processed batch", logOutput);
            Assert.Contains(batchNumber.ToString(), logOutput);
            Assert.Contains(rowCount.ToString(), logOutput);
        }

        [Fact]
        public void LogError_WithValidInputs_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var errorMessage = "Database connection failed";
            var exception = new InvalidOperationException("Test exception message");

            // Act
            _logger.LogError(errorMessage, exception);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL ERROR]", logOutput);
            Assert.Contains(errorMessage, logOutput);
            Assert.Contains(exception.Message, logOutput);
        }

        [Fact]
        public void LogTableCopyStarted_WithZeroRowCount_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var tableName = "EmptyTable";
            var rowCount = 0;

            // Act
            _logger.LogTableCopyStarted(tableName, rowCount);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("0", logOutput);
        }

        [Fact]
        public void LogTableCopyCompleted_WithZeroDuration_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var tableName = "FastTable";
            var rowCount = 100;
            var durationMs = 0L;

            // Act
            _logger.LogTableCopyCompleted(tableName, rowCount, durationMs);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("0ms", logOutput);
        }

        [Fact]
        public void LogBatchProcessed_WithFirstBatch_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var batchNumber = 1;
            var rowCount = 1000;

            // Act
            _logger.LogBatchProcessed(batchNumber, rowCount);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("batch 1", logOutput);
        }

        [Fact]
        public void LogError_WithNullException_HandlesGracefully()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var errorMessage = "Critical error";
            var exception = new Exception("Null reference error");

            // Act
            _logger.LogError(errorMessage, exception);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL ERROR]", logOutput);
            Assert.Contains(errorMessage, logOutput);
        }

        [Fact]
        public void LogStoredProcedureExecuted_WithZeroRowsAffected_WritesToConsole()
        {
            // Arrange
            var output = new StringWriter();
            Console.SetOut(output);
            var procedureName = "sp_NoOp";
            var rowsAffected = 0;

            // Act
            _logger.LogStoredProcedureExecuted(procedureName, rowsAffected);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[ETL]", logOutput);
            Assert.Contains("0 rows affected", logOutput);
        }
    }
}
