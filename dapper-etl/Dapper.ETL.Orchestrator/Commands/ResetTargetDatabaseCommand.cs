namespace Dapper.ETL.Orchestrator.Commands;

using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Truncates all target tables and resets their identity seeds.
/// </summary>
public class ResetTargetDatabaseCommand : Command
{
    private readonly DataService _dataService;

    private readonly static string[] TargetTables = [
        "dbo.CustomerCopy",
        "dbo.CustomerEmailList",
        "dbo.CustomerLoyaltyRewards"
    ];

    public ResetTargetDatabaseCommand(DataService dataService)
    {
        _dataService = dataService;
    }

    public override int Execute(CommandContext context)
    {
        try
        {
            AnsiConsole.Status()
                .Start("Resetting target database...", ctx =>
                {
                    _dataService.ResetTargetDatabase().GetAwaiter().GetResult();
                });

            AnsiConsole.MarkupLine(
                $"[green]Target database reset successfully. {TargetTables.Length} table(s) truncated and identity seeds reset.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to reset target database: {ex.Message}[/]");
            return 1;
        }
    }
}
