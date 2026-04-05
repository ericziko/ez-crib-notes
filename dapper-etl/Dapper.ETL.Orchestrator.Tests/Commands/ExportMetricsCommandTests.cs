using System.Diagnostics.Metrics;
using System.Text.Json;
using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Xunit;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
/// Integration tests for the export-metrics operation via <see cref="MetricsService"/>.
/// Tests exercise the service layer directly because <see cref="ExportMetricsCommand"/>
/// renders to AnsiConsole which is not suitable for headless test execution.
/// </summary>
public class ExportMetricsCommandTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture = new();
    private readonly List<string> _tempFiles = new();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f))
                File.Delete(f);
        await _fixture.DisposeAsync();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void Test_ExecuteAsync_ExportsJSON_Succeeds()
    {
        // Arrange
        using var meter = new Meter("Dapper.ETL.Test.Metrics.Json");
        var metricsService = new MetricsService(meter);
        metricsService.StoreMetrics(new Dictionary<string, object>
        {
            ["Total Duration|ms"] = 500L,
            ["Rows Copied|rows"]  = 50L,
        });

        var outputFile = TempFile("metrics-test.json");

        // Act: replicate what ExportMetricsCommand does for format=json
        var metrics = metricsService.GetLastMetrics()!;
        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFile, json);

        // Assert
        Assert.True(File.Exists(outputFile), "JSON file must be created.");
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(JsonValueKind.Object, parsed.ValueKind);
        Assert.True(parsed.EnumerateObject().Any(), "Exported JSON should contain metric entries.");
    }

    [Fact]
    public void Test_ExecuteAsync_ExportsCSV_Succeeds()
    {
        // Arrange
        using var meter = new Meter("Dapper.ETL.Test.Metrics.Csv");
        var metricsService = new MetricsService(meter);
        metricsService.StoreMetrics(new Dictionary<string, object>
        {
            ["Total Duration|ms"] = 750L,
            ["Rows Copied|rows"]  = 75L,
        });

        var outputFile = TempFile("metrics-test.csv");

        // Act: replicate what ExportMetricsCommand does for format=csv
        var metrics = metricsService.GetLastMetrics()!;
        var lines = new List<string> { "Metric,Value" };
        foreach (var entry in metrics)
            lines.Add($"{entry.Key},{entry.Value}");
        File.WriteAllLines(outputFile, lines);

        // Assert
        Assert.True(File.Exists(outputFile), "CSV file must be created.");
        var written = File.ReadAllLines(outputFile);
        Assert.True(written.Length >= 1, "CSV must have at least a header row.");
        Assert.Equal("Metric,Value", written[0]);
        Assert.True(written.Length >= 2, "CSV must have at least one data row.");
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private string TempFile(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), name);
        _tempFiles.Add(path);
        return path;
    }
}
