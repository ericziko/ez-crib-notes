using System.Text.Json;
using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

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