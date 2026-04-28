using System.ComponentModel;
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