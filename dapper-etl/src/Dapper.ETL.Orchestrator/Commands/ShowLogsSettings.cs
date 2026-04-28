using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

public sealed class ShowLogsSettings : CommandSettings {
    [Description("Minimum log level (Info|Warning|Error)")]
    [CommandOption("-l|--level")]
    [DefaultValue("Info")]
    public string Level { get; init; } = "Info";

    [Description("Maximum rows to display")]
    [CommandOption("-n|--limit")]
    [DefaultValue(100)]
    public int Limit { get; init; } = 100;
}