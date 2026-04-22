using System.ComponentModel;
using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Settings for the compare command.
/// </summary>
public class CompareDataSettings : CommandSettings {
    /// <summary>
    /// Gets or sets a value indicating whether to show only mismatched tables.
    /// </summary>
    [CommandOption("--mismatches-only")]
    [Description("Show only tables where source and target counts differ")]
    [DefaultValue(false)]
    public bool MismatchesOnly { get; set; }
}

/// <summary>
/// Command that compares row counts between source and target databases.
/// </summary>
public class CompareDataCommand : Command<CompareDataSettings> {
    private readonly ValidationService _validationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareDataCommand" /> class.
    /// </summary>
    public CompareDataCommand(ValidationService validationService) {
        _validationService = validationService;
    }

    /// <inheritdoc />
    protected override int Execute(CommandContext context, CompareDataSettings settings, CancellationToken cancellationToken) {
        try {
            AnsiConsole.MarkupLine("[grey]Comparing source vs target row counts...[/]");

            var (success, results) = _validationService.ValidateQuick().GetAwaiter().GetResult();

            if (results.Count == 0) {
                AnsiConsole.MarkupLine("[yellow]No comparison data available.[/]");
                return 0;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Table")
                .AddColumn(new TableColumn("Source").RightAligned())
                .AddColumn(new TableColumn("Target").RightAligned())
                .AddColumn(new TableColumn("Match %").RightAligned())
                .AddColumn("Status");

            foreach (var (tableName, detail) in results) {
                if (settings.MismatchesOnly && detail.Status == "OK") {
                    continue;
                }

                var statusMarkup = detail.Status == "OK"
                    ? $"[green]{detail.Status}[/]"
                    : $"[red]{detail.Status}[/]";

                table.AddRow(
                    tableName,
                    detail.SourceCount.ToString("N0"),
                    detail.TargetCount.ToString("N0"),
                    $"{detail.MatchPercent:F1}%",
                    statusMarkup);
            }

            AnsiConsole.Write(table);

            if (success) {
                AnsiConsole.MarkupLine("[green]All tables match.[/]");
            }
            else {
                AnsiConsole.MarkupLine("[red]Some tables have mismatches.[/]");
            }

            return success ? 0 : 1;
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Compare failed: {ex.Message}[/]");
            return 1;
        }
    }
}