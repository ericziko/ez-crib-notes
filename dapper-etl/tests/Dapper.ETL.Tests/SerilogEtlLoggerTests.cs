using System;
using Dapper.ETL.Library.Implementation;
using Moq;
using Serilog;
using Xunit;

namespace Dapper.ETL.Tests;

/// <summary>
/// Tests for the SerilogEtlLogger implementation.
/// </summary>
public class SerilogEtlLoggerTests {
    private readonly Mock<ILogger> _mockLogger;
    private readonly SerilogEtlLogger _sut;

    public SerilogEtlLoggerTests() {
        _mockLogger = new Mock<ILogger>();
        // Serilog's ForContext returns another ILogger; stub it so internal calls don't NPE.
        _mockLogger
            .Setup(l => l.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
            .Returns(_mockLogger.Object);
        _sut = new SerilogEtlLogger(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithInjectedLogger_DoesNotThrow() {
        // Act & Assert - should not throw
        var logger = new SerilogEtlLogger(_mockLogger.Object);
        Assert.NotNull(logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => new SerilogEtlLogger(null!));
    }

    [Fact]
    public void LogTableCopyCompleted_EmitsInformationWithStructuredProperties() {
        // Arrange
        const string sourceTable = "Users";
        const int rowCount = 500;
        const long durationMs = 1234L;

        // Act
        _sut.LogTableCopyCompleted(sourceTable, rowCount, durationMs);

        // Assert: Serilog ILogger.Information is called with the message template
        // containing {TableName}, {RowCount}, {DurationMs} tokens.
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(t => t.Contains("{TableName}") && t.Contains("{RowCount}") && t.Contains("{DurationMs}")),
                It.Is<string>(v => v == sourceTable),
                It.Is<int>(v => v == rowCount),
                It.Is<long>(v => v == durationMs)),
            Times.Once);
    }

    [Fact]
    public void LogError_EmitsErrorWithExceptionAndErrorMessageProperty() {
        // Arrange
        const string errorMessage = "Something went wrong";
        var exception = new InvalidOperationException("inner failure");

        // Act
        _sut.LogError(errorMessage, exception);

        // Assert: Error overload that accepts an Exception is called
        _mockLogger.Verify(
            l => l.Error(
                It.Is<Exception>(e => e == exception),
                It.Is<string>(t => t.Contains("{ErrorMessage}")),
                It.Is<string>(v => v == errorMessage)),
            Times.Once);
    }

    [Fact]
    public void LogTableCopyStarted_EmitsInformationWithTableNameAndRowCount() {
        // Arrange
        const string tableName = "Orders";
        const int rowCount = 100;

        // Act
        _sut.LogTableCopyStarted(tableName, rowCount);

        // Assert
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(t => t.Contains("{TableName}") && t.Contains("{RowCount}")),
                It.Is<string>(v => v == tableName),
                It.Is<int>(v => v == rowCount)),
            Times.Once);
    }
}