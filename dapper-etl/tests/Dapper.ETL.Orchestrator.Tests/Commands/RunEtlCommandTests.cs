namespace Dapper.ETL.Orchestrator.Tests.Commands;

using Dapper.ETL.Library.Models;
using Dapper.ETL.Orchestrator.Commands;
using Dapper.ETL.Orchestrator.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Unit tests for EtlService.RunEtl (used by RunEtlCommand).
/// RunEtl is a stub returning success with 0 rows — no database connection required.
/// </summary>
public class RunEtlCommandTests
{
    private static EtlService BuildEtlService()
        => new("Server=.;Database=Source;Trusted_Connection=True", NullLogger<EtlService>.Instance);

    [Fact]
    public async Task Test_RunEtl_AtomicMode_ReturnsSuccess()
    {
        // Arrange
        var etlService = BuildEtlService();

        // Act
        var result = await etlService.RunEtl(EtlTransactionMode.Atomic);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.RowsCopied);
        Assert.Equal(0, result.Duration);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Test_RunEtl_PartialMode_ReturnsSuccess()
    {
        // Arrange
        var etlService = BuildEtlService();

        // Act
        var result = await etlService.RunEtl(EtlTransactionMode.Partial);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.RowsCopied);
        Assert.Equal(0, result.Duration);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Test_RunEtlSettings_DefaultAtomic_IsTrue()
    {
        // Arrange / Act
        var settings = new RunEtlSettings();

        // Assert
        Assert.True(settings.Atomic, "Default transaction mode should be Atomic (true).");
    }

    [Fact]
    public void Test_RunEtlSettings_CanSetAtomicFalse()
    {
        // Arrange / Act
        var settings = new RunEtlSettings { Atomic = false };

        // Assert
        Assert.False(settings.Atomic);
    }

    [Fact]
    public void Test_EtlService_CanBeConstructed()
    {
        // Arrange / Act
        var exception = Record.Exception(BuildEtlService);

        // Assert
        Assert.Null(exception);
    }
}
