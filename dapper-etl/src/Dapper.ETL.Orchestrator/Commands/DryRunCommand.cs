using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

using Dapper.ETL.Orchestrator.Services;

/// <summary>
/// Shows a dry-run execution plan for the ETL without committing any data.
/// </summary>
public class DryRunCommand : Command
{
    private const double ThroughputRowsPerSec = 20.0;

    private readonly MetricsService _metricsService;

    public DryRunCommand(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public override int Execute(CommandContext context)
    {
        AnsiConsole.MarkupLine("[bold]ETL Execution Plan (DRY RUN)[/]");
        AnsiConsole.MarkupLine($"Source table: [cyan]Customer[/]");
        AnsiConsole.MarkupLine($"Target tables: [cyan]Copy[/], [cyan]EmailList[/], [cyan]LoyaltyRewards[/]");
        AnsiConsole.WriteLine();

        // Simulate source row count (no actual DB query in dry-run)
        const int estimatedRowCount = 100;

        var targets = new[]
        {
            new DryRunTarget("Copy",           estimatedRowCount, "None"),
            new DryRunTarget("EmailList",      estimatedRowCount, "LOWER(Email), TRIM(Name)"),
            new DryRunTarget("LoyaltyRewards", estimatedRowCount, "TRIM(Name)"),
        };

        var table = new Table();
        table.AddColumn("TargetTable");
        table.AddColumn("RowCount (estimated)");
        table.AddColumn("Duration (estimated)");
        table.AddColumn("Transforms");

        foreach (var t in targets)
        {
            var durationSec = t.RowCount / ThroughputRowsPerSec;
            table.AddRow(
                t.TableName,
                t.RowCount.ToString(),
                $"{durationSec:F0}s",
                t.Transforms);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[yellow]Execution skipped (DRY RUN)[/]");
        return 0;
    }

    private sealed record DryRunTarget(string TableName, int RowCount, string Transforms);
}
