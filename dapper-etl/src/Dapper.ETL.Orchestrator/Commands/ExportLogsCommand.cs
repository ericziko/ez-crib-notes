using System.ComponentModel;
using System.Text.Json;
using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Settings for the export-logs command.
/// </summary>
public class ExportLogsSettings : CommandSettings {
    /// <summary>
    /// Gets or sets the output file path for the exported logs.
    /// </summary>
    [CommandArgument(0, "[output]")]
    [Description("Output file path (default: etl-logs.json)")]
    public string Output { get; set; } = "etl-logs.json";

    /// <summary>
    /// Gets or sets the minimum log level to include.
    /// </summary>
    [CommandOption("--level")]
    [Description("Minimum log level: Info, Warning, Error")]
    [DefaultValue("Info")]
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the maximum number of log entries to export.
    /// </summary>
    [CommandOption("--limit")]
    [Description("Maximum number of entries to export")]
    [DefaultValue(1000)]
    public int Limit { get; set; } = 1000;
}

/// <summary>
/// Command that exports log entries to a JSON file.
/// </summary>
public class ExportLogsCommand : Command<ExportLogsSettings> {
    private readonly LoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportLogsCommand" /> class.
    /// </summary>
    public ExportLogsCommand(LoggingService loggingService) {
        _loggingService = loggingService;
    }

    /// <inheritdoc />
    protected override int Execute(CommandContext context, ExportLogsSettings settings, CancellationToken cancellationToken) {
        try {
            AnsiConsole.MarkupLine($"[grey]Exporting logs (level: {settings.Level}, limit: {settings.Limit})...[/]");

            var logs = _loggingService.GetLogs(settings.Level, settings.Limit).GetAwaiter().GetResult();

            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settings.Output, json);

            AnsiConsole.MarkupLine($"[green]Exported {logs.Count} log entries to [bold]{settings.Output}[/][/]");
            return 0;
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Export logs failed: {ex.Message}[/]");
            return 1;
        }
    }
}