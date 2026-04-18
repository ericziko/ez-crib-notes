namespace Dapper.ETL.Orchestrator.Commands;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Truncates the EtlLogs.dbo.Logs table after user confirmation.
/// </summary>
public class ClearLogsCommand : Command
{
    private readonly IConfiguration _configuration;

    public ClearLogsCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override int Execute(CommandContext context)
    {
        var confirmed = AnsiConsole.Confirm("Clear all logs? This cannot be undone.");
        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled. No logs were cleared.[/]");
            return 0;
        }

        try
        {
            var connectionString = _configuration["ConnectionStrings:Logs"]
                ?? throw new InvalidOperationException("ConnectionStrings:Logs is not configured.");

            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand("TRUNCATE TABLE dbo.Logs", connection);
            command.ExecuteNonQuery();

            AnsiConsole.MarkupLine("[green]All logs cleared successfully.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to clear logs: {ex.Message}[/]");
            return 1;
        }
    }
}
