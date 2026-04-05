using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

using Dapper.ETL.Orchestrator.Services;

/// <summary>
/// Displays metrics from the last ETL run as a Spectre.Console table.
/// </summary>
public class GetStatsCommand : Command
{
    private readonly MetricsService _metricsService;

    public GetStatsCommand(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public override int Execute(CommandContext context)
    {
        var metrics = _metricsService.GetLastMetrics();

        if (metrics == null || metrics.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ETL run recorded[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.AddColumn("Unit");

        foreach (var entry in metrics)
        {
            // Each key encodes the unit after a '|' separator, e.g. "Total Duration|ms"
            var parts = entry.Key.Split('|');
            var metricName = parts[0];
            var unit = parts.Length > 1 ? parts[1] : string.Empty;
            table.AddRow(metricName, entry.Value?.ToString() ?? string.Empty, unit);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
