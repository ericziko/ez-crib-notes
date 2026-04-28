using System.ComponentModel;
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