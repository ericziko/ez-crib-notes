using System.ComponentModel;
using Dapper.ETL.Orchestrator.Services;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

public sealed class ValidateDataSettings : CommandSettings {
    [Description("Validation level (quick|standard|thorough)")]
    [CommandOption("-l|--level")]
    [DefaultValue("quick")]
    public string Level { get; init; } = "quick";
}

/// <summary>
/// Validates ETL data integrity between source and target databases.
/// Dispatches to quick (row counts), standard (schema), or thorough (MD5 + duplicates).
/// </summary>
public sealed class ValidateDataCommand : Command<ValidateDataSettings> {
    private readonly ValidationService _validationService;

    public ValidateDataCommand(IConfiguration configuration) {
        _validationService = new ValidationService(configuration);
    }

    protected override int Execute(CommandContext context, ValidateDataSettings settings, CancellationToken cancellationToken) {
        var level = settings.Level.ToLowerInvariant();

        AnsiConsole.MarkupLine($"[bold]Running [cyan]{level}[/] validation...[/]");

        var (success, results) = level switch {
            "standard" => RunAsync(() => _validationService.ValidateStandard()),
            "thorough" => RunAsync(() => _validationService.ValidateThorough()),
            _ => RunAsync(() => _validationService.ValidateQuick())
        };

        var table = new Table()
            .AddColumn("TableName")
            .AddColumn(new TableColumn("SourceCount").RightAligned())
            .AddColumn(new TableColumn("TargetCount").RightAligned())
            .AddColumn(new TableColumn("Match%").RightAligned())
            .AddColumn("Status");

        foreach (var detail in results.Values) {
            var statusMarkup = detail.Status == "OK"
                ? $"[green]{detail.Status}[/]"
                : $"[red]{detail.Status}[/]";

            table.AddRow(
                detail.TableName,
                detail.SourceCount.ToString(),
                detail.TargetCount.ToString(),
                $"{detail.MatchPercent:F1}%",
                statusMarkup);
        }

        AnsiConsole.Write(table);

        if (!success) {
            AnsiConsole.MarkupLine("[red]Validation failed: one or more mismatches detected.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Validation passed.[/]");
        return 0;
    }

    private static (bool Success, Dictionary<string, ValidationDetail> Results) RunAsync(
        Func<Task<(bool, Dictionary<string, ValidationDetail>)>> fn) {
        return fn().GetAwaiter().GetResult();
    }
}