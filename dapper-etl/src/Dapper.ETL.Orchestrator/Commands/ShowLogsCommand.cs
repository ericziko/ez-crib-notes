using System.ComponentModel;
using Dapper.ETL.Orchestrator.Services;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

public sealed class ShowLogsSettings : CommandSettings {
    [Description("Minimum log level (Info|Warning|Error)")]
    [CommandOption("-l|--level")]
    [DefaultValue("Info")]
    public string Level { get; init; } = "Info";

    [Description("Maximum rows to display")]
    [CommandOption("-n|--limit")]
    [DefaultValue(100)]
    public int Limit { get; init; } = 100;
}

/// <summary>
/// Displays structured log entries from EtlLogs.dbo.Logs filtered by level and row limit.
/// </summary>
public sealed class ShowLogsCommand : Command<ShowLogsSettings> {
    private readonly LoggingService _loggingService;

    public ShowLogsCommand(IConfiguration configuration) {
        _loggingService = new LoggingService(configuration);
    }

    protected override int Execute(CommandContext context, ShowLogsSettings settings, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine(
            $"[bold]Fetching logs — level:[cyan]{settings.Level}[/] limit:[cyan]{settings.Limit}[/][/]");

        List<LogEntry> logs;
        try {
            logs = _loggingService
                .GetLogs(settings.Level, settings.Limit)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error querying logs: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }

        var table = new Table()
            .AddColumn("TimeStamp")
            .AddColumn("Level")
            .AddColumn("MessageTemplate")
            .AddColumn("Properties");

        foreach (var entry in logs) {
            var levelMarkup = entry.Level switch {
                "Error" or "Fatal" => $"[red]{Markup.Escape(entry.Level)}[/]",
                "Warning" => $"[yellow]{Markup.Escape(entry.Level)}[/]",
                _ => $"[green]{Markup.Escape(entry.Level)}[/]"
            };

            // Truncate Properties JSON to keep the table readable
            var props = entry.Properties ?? string.Empty;
            if (props.Length > 80) {
                props = string.Concat(props.AsSpan(0, 77), "...");
            }

            table.AddRow(
                Markup.Escape(entry.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss")),
                levelMarkup,
                Markup.Escape(entry.MessageTemplate),
                Markup.Escape(props));
        }

        AnsiConsole.Write(table);
        return 0;
    }
}