using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Settings for the seed-source-customers command.
/// </summary>
public class SeedSourceCustomersSettings : CommandSettings {
    /// <summary>
    /// Gets or sets the number of customers to seed.
    /// </summary>
    [CommandArgument(0, "<count>")]
    [Description("Number of customers to seed")]
    public int Count { get; set; }
}