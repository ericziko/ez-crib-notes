using System.ComponentModel;
using Dapper.ETL.Library.Models;
using Dapper.ETL.Orchestrator.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Commands;

/// <summary>
///     Settings for the run-etl command.
/// </summary>
public class RunEtlSettings : CommandSettings {
    /// <summary>
    ///     Gets or sets a value indicating whether to run the ETL in atomic (all-or-nothing) mode.
    /// </summary>
    [CommandOption("--atomic")]
    [Description("Use atomic transaction (all-or-nothing)")]
    [DefaultValue(true)]
    public bool Atomic { get; set; } = true;
}

/// <summary>
///     Command that executes the ETL pipeline.
/// </summary>
public class RunEtlCommand : Command<RunEtlSettings> {
    private readonly EtlService _etlService;
    private readonly ILogger<RunEtlCommand> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RunEtlCommand" /> class.
    /// </summary>
    public RunEtlCommand(EtlService etlService, ILogger<RunEtlCommand> logger) {
        _etlService = etlService;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override int Execute(CommandContext context, RunEtlSettings settings, CancellationToken cancellationToken) {
        var mode = settings.Atomic ? EtlTransactionMode.Atomic : EtlTransactionMode.Partial;

        AnsiConsole.MarkupLine($"[grey]Running ETL in [bold]{mode}[/] mode...[/]");

        try {
            var result = _etlService.RunEtl(mode).GetAwaiter().GetResult();

            if (result.Success) {
                AnsiConsole.MarkupLine($"[green]✓ ETL completed successfully. Rows copied: {result.RowsCopied}, Duration: {result.Duration}ms[/]");
                return 0;
            }
            AnsiConsole.MarkupLine($"[red]✗ ETL failed: {result.ErrorMessage}[/]");
            return 1;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "ETL command failed unexpectedly");
            AnsiConsole.MarkupLine($"[red]✗ ETL failed: {ex.Message}[/]");
            return 1;
        }
    }
}