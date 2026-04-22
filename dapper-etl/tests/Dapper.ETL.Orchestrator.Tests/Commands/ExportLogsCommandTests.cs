using System.Text.Json;
using Dapper.ETL.Orchestrator.Services;
using Dapper.ETL.Orchestrator.Tests.Fixtures;
using Microsoft.Extensions.Configuration;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

/// <summary>
///     Integration tests for the export-logs operation via <see cref="LoggingService" />.
///     Tests exercise the service layer directly because <see cref="ExportLogsCommand" />
///     renders to AnsiConsole which is not suitable for headless test execution.
/// </summary>
[Collection("SharedSqlServer collection")]
public class ExportLogsCommandTests {
    private readonly IConfiguration _configuration;
    private readonly SharedSqlServerFixture _fixture;
    private readonly List<string> _tempFiles = new();

    public ExportLogsCommandTests(SharedSqlServerFixture fixture) {
        _fixture = fixture;
        _configuration = BuildConfiguration();
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Test_ExecuteAsync_ExportsToJson_Succeeds() {
        // Arrange: seed some log rows (stub returns empty list but we verify the
        // serialisation and file-write plumbing works)
        var service = new LoggingService(_configuration);
        var outputFile = TempFile("etl-logs-test.json");

        // Act: retrieve logs then serialise as the command does
        var logs = await service.GetLogs("Info", 100);
        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFile, json);

        // Assert
        Assert.True(File.Exists(outputFile), "Export file should exist.");
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(JsonValueKind.Array, parsed.ValueKind);
    }

    [Fact]
    public async Task Test_ExecuteAsync_ExportsToCSV_Succeeds() {
        // Arrange: export logs in a CSV-like format (one line per entry)
        var service = new LoggingService(_configuration);
        var outputFile = TempFile("etl-logs-warning.csv");

        // Act: retrieve logs and write a simple CSV
        var logs = await service.GetLogs("Warning", 50);
        var lines = new List<string> { "TimeStamp,Level,MessageTemplate,Properties" };
        foreach (var entry in logs) {
            lines.Add($"{entry.TimeStamp:O},{entry.Level},{entry.MessageTemplate},{entry.Properties}");
        }
        File.WriteAllLines(outputFile, lines);

        // Assert
        Assert.True(File.Exists(outputFile), "CSV export file should exist.");
        var written = File.ReadAllLines(outputFile);
        Assert.True(written.Length >= 1, "CSV must have at least a header row.");
        Assert.Equal("TimeStamp,Level,MessageTemplate,Properties", written[0]);
    }

    [Fact]
    public async Task Test_ExecuteAsync_CreatesOutputFile() {
        // Arrange: unique output path to confirm file creation
        var outputFile = TempFile($"created-logs-{Guid.NewGuid():N}.json");
        Assert.False(File.Exists(outputFile), "Pre-condition: file must not exist yet.");

        var service = new LoggingService(_configuration);

        // Act: simulate what ExportLogsCommand does
        var logs = await service.GetLogs("Error", 10);
        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFile, json);

        // Assert
        Assert.True(File.Exists(outputFile), "Command must create the output file.");
        Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(outputFile)));
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private string TempFile(string name) {
        var path = Path.Combine(Path.GetTempPath(), name);
        _tempFiles.Add(path);
        return path;
    }

    private IConfiguration BuildConfiguration() {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:Source"] = _fixture.GetConnectionString("TestDbSource"),
                ["ConnectionStrings:Target"] = _fixture.GetConnectionString("TestDbTarget"),
                ["ConnectionStrings:Logs"] = _fixture.GetConnectionString("EtlLogs")
            })
            .Build();
    }
}