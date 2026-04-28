using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Settings for <see cref="ExportMetricsCommand" />.
/// </summary>
public class ExportMetricsSettings : CommandSettings {
    [Description("Export format (csv|json)")]
    [DefaultValue("json")]
    public string Format { get; init; } = "json";

    [Description("Output file path (optional)")]
    public string? OutputFile { get; init; }
}