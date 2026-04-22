using Dapper.ETL.Orchestrator.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
///     Displays row counts for all ETL-related tables and the last ETL run info.
/// </summary>
public class StatusCommand : Command {

    private readonly static (string Database, string Table, string DisplayName)[] TableSources = new[] {
        ("Source", "dbo.Customer", "Customer"),
        ("Target", "dbo.CustomerCopy", "CustomerCopy"),
        ("Target", "dbo.CustomerEmailList", "CustomerEmailList"),
        ("Target", "dbo.CustomerLoyaltyRewards", "CustomerLoyaltyRewards"),
        ("Logs", "dbo.Logs", "Logs")
    };

    private readonly DataService _dataService;

    public StatusCommand(DataService dataService) {
        _dataService = dataService;
    }

    protected override int Execute(CommandContext context, CancellationToken cancellationToken) {
        try {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Table Name")
                .AddColumn("Database")
                .AddColumn(new TableColumn("Row Count").RightAligned());

            foreach (var (database, tableName, displayName) in TableSources) {
                var count = _dataService.GetRowCount(database, tableName).GetAwaiter().GetResult();
                table.AddRow(displayName, database, count.ToString("N0"));
            }

            AnsiConsole.Write(table);

            var lastRun = _dataService.GetLastEtlRunInfo().GetAwaiter().GetResult();
            if (lastRun is not null) {
                AnsiConsole.MarkupLine($"\n[bold]Last ETL Run:[/] {lastRun.RunAt:yyyy-MM-dd HH:mm:ss}  " +
                                       $"Duration: {lastRun.DurationMs}ms  " +
                                       $"Rows: {lastRun.RowsCopied:N0}  " +
                                       $"Status: {(lastRun.Success ? "[green]Success[/]" : "[red]Failed[/]")}");
            }
            else {
                AnsiConsole.MarkupLine("\n[grey]No ETL run info available yet.[/]");
            }

            return 0;
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Status query failed: {ex.Message}[/]");
            return 1;
        }
    }
}