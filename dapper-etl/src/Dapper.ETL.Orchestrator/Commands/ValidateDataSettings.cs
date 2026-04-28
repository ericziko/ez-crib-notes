using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

public sealed class ValidateDataSettings : CommandSettings {
    [Description("Validation level (quick|standard|thorough)")]
    [CommandOption("-l|--level")]
    [DefaultValue("quick")]
    public string Level { get; init; } = "quick";
}