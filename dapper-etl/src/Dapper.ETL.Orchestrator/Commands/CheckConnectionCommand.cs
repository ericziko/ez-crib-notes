using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Command that checks connectivity to all configured databases.
/// </summary>
public class CheckConnectionCommand : Command {
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckConnectionCommand" /> class.
    /// </summary>
    public CheckConnectionCommand(IConfiguration configuration) {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override int Execute(CommandContext context, CancellationToken cancellationToken) {
        var databases = new[] {
            ("Source", _configuration["ConnectionStrings:Source"]),
            ("Target", _configuration["ConnectionStrings:Target"]),
            ("Logs", _configuration["ConnectionStrings:Logs"])
        };

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Database")
            .AddColumn("Status")
            .AddColumn("Detail");

        var allOk = true;

        foreach (var (name, connectionString) in databases) {
            if (string.IsNullOrWhiteSpace(connectionString)) {
                table.AddRow(name, "[yellow]MISSING[/]", "Connection string not configured");
                allOk = false;
                continue;
            }

            try {
                using var conn = new SqlConnection(connectionString);
                conn.Open();
                using var cmd = new SqlCommand("SELECT @@SERVERNAME", conn);
                var serverName = cmd.ExecuteScalar()?.ToString() ?? "unknown";
                table.AddRow(name, "[green]OK[/]", $"Server: {serverName}");
            }
            catch (Exception ex) {
                table.AddRow(name, "[red]FAILED[/]", ex.Message);
                allOk = false;
            }
        }

        AnsiConsole.Write(table);
        return allOk ? 0 : 1;
    }
}