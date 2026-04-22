using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Default command shown when no sub-command is specified.
/// Displays ETL status and available commands.
/// </summary>
public class DefaultCommand : Command {
    private readonly DataService _dataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCommand" /> class.
    /// </summary>
    public DefaultCommand(DataService dataService) {
        _dataService = dataService;
    }

    public int Execute(CommandContext context) {
        return Execute(context, CancellationToken.None);
    }
    /// <inheritdoc />
    protected override int Execute(CommandContext context, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine("[bold cyan]Dapper ETL Orchestrator[/]");
        AnsiConsole.MarkupLine("[grey]Use one of the commands below to interact with the ETL pipeline.[/]");
        AnsiConsole.WriteLine();

        var commandsTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Command")
            .AddColumn("Description");

        commandsTable.AddRow("[green]run[/]", "Execute the ETL pipeline");
        commandsTable.AddRow("[green]seed[/]", "Seed source database with test customers");
        commandsTable.AddRow("[green]validate[/]", "Validate data integrity");
        commandsTable.AddRow("[green]logs[/]", "Show recent log entries");
        commandsTable.AddRow("[green]clear-logs[/]", "Clear the logs database");
        commandsTable.AddRow("[green]reset[/]", "Reset target database tables");
        commandsTable.AddRow("[green]status[/]", "Show row counts and last ETL run info");
        commandsTable.AddRow("[green]compare[/]", "Compare source vs target row counts");
        commandsTable.AddRow("[green]export-logs[/]", "Export logs to JSON file");
        commandsTable.AddRow("[green]check[/]", "Check database connectivity");
        commandsTable.AddRow("[green]stats[/]", "Show ETL statistics");
        commandsTable.AddRow("[green]export-metrics[/]", "Export metrics to file");
        commandsTable.AddRow("[green]dry-run[/]", "Preview ETL without writing data");

        AnsiConsole.Write(commandsTable);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Run [bold]<command> --help[/] for command-specific options.[/]");

        return 0;
    }
}