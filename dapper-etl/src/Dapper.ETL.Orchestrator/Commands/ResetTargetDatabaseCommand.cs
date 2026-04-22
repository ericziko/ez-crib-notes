using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
///     Truncates all target tables and resets their identity seeds.
/// </summary>
public class ResetTargetDatabaseCommand : Command {

    private readonly static string[] TargetTables = [
        "dbo.CustomerCopy",
        "dbo.CustomerEmailList",
        "dbo.CustomerLoyaltyRewards"
    ];

    private readonly DataService _dataService;

    public ResetTargetDatabaseCommand(DataService dataService) {
        _dataService = dataService;
    }

    protected override int Execute(CommandContext context, CancellationToken cancellationToken) {
        try {
            AnsiConsole.Status()
                .Start("Resetting target database...", ctx => { _dataService.ResetTargetDatabase().GetAwaiter().GetResult(); });

            AnsiConsole.MarkupLine(
                $"[green]Target database reset successfully. {TargetTables.Length} table(s) truncated and identity seeds reset.[/]");
            return 0;
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Failed to reset target database: {ex.Message}[/]");
            return 1;
        }
    }
}