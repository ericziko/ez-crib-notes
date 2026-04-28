using Dapper.ETL.Orchestrator.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
/// Command that seeds test customer rows into the source database.
/// </summary>
public class SeedSourceCustomersCommand : Command<SeedSourceCustomersSettings> {
    private readonly EtlService _etlService;
    private readonly ILogger<SeedSourceCustomersCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedSourceCustomersCommand" /> class.
    /// </summary>
    public SeedSourceCustomersCommand(EtlService etlService, ILogger<SeedSourceCustomersCommand> logger) {
        _etlService = etlService;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override int Execute(CommandContext context, SeedSourceCustomersSettings settings, CancellationToken cancellationToken) {
        AnsiConsole.MarkupLine($"[grey]Seeding [bold]{settings.Count}[/] customers into source database...[/]");

        try {
            var inserted = 0;

            AnsiConsole.Status()
                .Start("Inserting customers...", ctx => {
                    ctx.Spinner(Spinner.Known.Dots);
                    inserted = _etlService.SeedCustomers(settings.Count).GetAwaiter().GetResult();
                });

            AnsiConsole.MarkupLine($"[green]✓ Inserted {inserted} customer(s) successfully.[/]");
            return 0;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "SeedSourceCustomers command failed unexpectedly");
            AnsiConsole.MarkupLine($"[red]✗ Seed failed: {ex.Message}[/]");
            return 1;
        }
    }
}