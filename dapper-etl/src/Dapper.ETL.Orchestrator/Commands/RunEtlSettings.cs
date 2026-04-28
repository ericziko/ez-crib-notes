using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Settings for the run-etl command.
/// </summary>
public class RunEtlSettings : CommandSettings {
    /// <summary>
    /// Gets or sets a value indicating whether to run the ETL in atomic (all-or-nothing) mode.
    /// </summary>
    [CommandOption("--atomic")]
    [Description("Use atomic transaction (all-or-nothing)")]
    [DefaultValue(true)]
    public bool Atomic { get; set; } = true;
}