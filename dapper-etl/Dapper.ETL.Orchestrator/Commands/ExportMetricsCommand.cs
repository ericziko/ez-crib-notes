using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

using Dapper.ETL.Orchestrator.Services;

/// <summary>
/// Settings for <see cref="ExportMetricsCommand"/>.
/// </summary>
public class ExportMetricsSettings : CommandSettings
{
    [System.ComponentModel.Description("Export format (csv|json)")]
    [System.ComponentModel.DefaultValue("json")]
    public string Format { get; init; } = "json";

    [System.ComponentModel.Description("Output file path (optional)")]
    public string? OutputFile { get; init; }
}

/// <summary>
/// Exports the last ETL metrics to a file in csv or json format.
/// </summary>
public class ExportMetricsCommand : Command<ExportMetricsSettings>
{
    private readonly MetricsService _metricsService;

    public ExportMetricsCommand(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public override int Execute(CommandContext context, ExportMetricsSettings settings)
    {
        var format = (settings.Format ?? "json").ToLowerInvariant();
        var outputFile = settings.OutputFile ?? $"metrics.{format}";

        var metrics = _metricsService.GetLastMetrics();
        if (metrics == null || metrics.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ETL metrics available to export.[/]");
            return 1;
        }

        try
        {
            if (format == "json")
            {
                var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(outputFile, json);
            }
            else if (format == "csv")
            {
                var lines = new List<string> { "Metric,Value" };
                foreach (var entry in metrics)
                {
                    lines.Add($"{entry.Key},{entry.Value}");
                }
                File.WriteAllLines(outputFile, lines);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Unknown format '{format}'. Use 'csv' or 'json'.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Exported {metrics.Count} metrics to[/] [bold]{outputFile}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Export failed: {ex.Message}[/]");
            return 1;
        }
    }
}
