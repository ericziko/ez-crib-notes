using System.Diagnostics.Metrics;
using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for the get-stats operation via <see cref="MetricsService"/>.
/// Tests exercise the service layer directly because <see cref="GetStatsCommand"/>
/// renders to AnsiConsole which is not suitable for headless test execution.
/// </summary>
[Collection("SharedSqlServer collection")]
public class GetStatsCommandTests
{
    private readonly SharedSqlServerFixture _fixture;

    public GetStatsCommandTests(SharedSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void Test_ExecuteAsync_WithMetrics_DisplaysValues()
    {
        // Arrange: store a metrics snapshot so GetLastMetrics returns data
        using var meter = new Meter("Dapper.ETL.Test.Stats");
        var metricsService = new MetricsService(meter);
        metricsService.StoreMetrics(new Dictionary<string, object>
        {
            ["Total Duration|ms"]   = 1234L,
            ["Rows Copied|rows"]    = 100L,
            ["Throughput|rows/sec"] = 81.0,
        });

        // Act
        var metrics = metricsService.GetLastMetrics();

        // Assert: non-null, non-empty metrics available
        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
        Assert.True(metrics.ContainsKey("Total Duration|ms"));
        Assert.Equal(1234L, metrics["Total Duration|ms"]);
    }

    [Fact]
    public void Test_ExecuteAsync_WithoutMetrics_ShowsNotRecorded()
    {
        // Arrange: fresh MetricsService with no stored metrics
        using var meter = new Meter("Dapper.ETL.Test.Stats.Empty");
        var metricsService = new MetricsService(meter);
        // Deliberately do NOT call StoreMetrics

        // Act
        var metrics = metricsService.GetLastMetrics();

        // Assert: null metrics → command would display "No ETL run recorded"
        Assert.Null(metrics);
    }
}
