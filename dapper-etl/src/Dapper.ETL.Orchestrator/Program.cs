using System.Diagnostics.Metrics;
using Dapper.ETL.Orchestrator.Commands;
using Dapper.ETL.Orchestrator.Infrastructure;
using Dapper.ETL.Orchestrator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.MSSqlServer;
using Spectre.Console.Cli;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, false)
    .AddJsonFile($"appsettings.{env}.json", true, false)
    .AddEnvironmentVariables()
    .Build();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .Enrich.WithProperty("Application", "ETL.Orchestrator")
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName();

var logsConnStr = SqlConnectionExtensions.AssembleConnectionString(
    configuration, "ConnectionStrings:Logs", "LogsCred");

if (!string.IsNullOrWhiteSpace(logsConnStr)) {
    loggerConfig = loggerConfig.WriteTo.MSSqlServer(
        logsConnStr,
        new MSSqlServerSinkOptions {
            TableName = "Logs",
            SchemaName = "dbo",
            AutoCreateSqlTable = true
        });
}

var seqUrl = configuration["Seq:Url"];
if (!string.IsNullOrWhiteSpace(seqUrl)) {
    loggerConfig = loggerConfig.WriteTo.Seq(seqUrl);
}

Log.Logger = loggerConfig.CreateLogger();

var isAspire = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPIRE_ENVIRONMENT"))
               || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

try {
    Log.Information("ETL.Orchestrator starting up (Environment: {Environment})", env);

    if (isAspire) {
        Log.Information("Running under Aspire orchestration");
    }
    else {
        Log.Information("Running standalone (local docker-compose or manual)");
    }

    // Build DI service collection
    var services = new ServiceCollection();

    // Configuration
    services.AddSingleton<IConfiguration>(configuration);

    // Logging — wire Serilog into Microsoft.Extensions.Logging
    services.AddLogging(builder => {
        builder.ClearProviders();
        builder.AddProvider(new SerilogLoggerProvider(Log.Logger, false));
    });

    // Keyed SQL Server connection strings
    var startupLogger = new SerilogLoggerProvider(Log.Logger, false)
        .CreateLogger("SqlConnections");

    services.AddKeyedSqlConnections(configuration, startupLogger, connections => {
        connections.Add("Source", "ConnectionStrings:Source", "SourceCred");
        connections.Add("Target", "ConnectionStrings:Target", "TargetCred");
        connections.Add("Logs", "ConnectionStrings:Logs", "LogsCred");
    });

    // Application services
    services.AddSingleton<MetricsService>(sp => {
        var meter = new Meter("Dapper.ETL");
        var logger = sp.GetService<ILogger<MetricsService>>();
        return new MetricsService(meter, logger);
    });
    services.AddSingleton<EtlService>();
    services.AddSingleton<ValidationService>();
    services.AddSingleton<DataService>();
    services.AddSingleton<LoggingService>();

    // Create type registrar wrapping the service collection
    var registrar = new TypeRegistrar(services);

    // Build and configure command app
    var app = new CommandApp<DefaultCommand>(registrar);

    app.Configure(config => {
        config.SetApplicationName("etl");
        config.AddCommand<RunEtlCommand>("run")
            .WithDescription("Execute the ETL pipeline");
        config.AddCommand<SeedSourceCustomersCommand>("seed")
            .WithDescription("Seed source database with test customers");
        config.AddCommand<ValidateDataCommand>("validate")
            .WithDescription("Validate data integrity between source and target");
        config.AddCommand<ShowLogsCommand>("logs")
            .WithDescription("Show recent log entries");
        config.AddCommand<ClearLogsCommand>("clear-logs")
            .WithDescription("Clear the logs database");
        config.AddCommand<ResetTargetDatabaseCommand>("reset")
            .WithDescription("Reset target database tables");
        config.AddCommand<StatusCommand>("status")
            .WithDescription("Show row counts and last ETL run info");
        config.AddCommand<CompareDataCommand>("compare")
            .WithDescription("Compare source vs target row counts");
        config.AddCommand<ExportLogsCommand>("export-logs")
            .WithDescription("Export logs to a JSON file");
        config.AddCommand<CheckConnectionCommand>("check")
            .WithDescription("Check connectivity to all configured databases");
        config.AddCommand<GetStatsCommand>("stats")
            .WithDescription("Show ETL statistics");
        config.AddCommand<ExportMetricsCommand>("export-metrics")
            .WithDescription("Export metrics to file");
        config.AddCommand<DryRunCommand>("dry-run")
            .WithDescription("Preview ETL without writing data");
    });

    var exitCode = await app.RunAsync(args);

    Log.Information("ETL.Orchestrator completed with exit code {ExitCode}", exitCode);
    return exitCode;
}
catch (Exception ex) {
    Log.Fatal(ex, "ETL.Orchestrator terminated unexpectedly");
    return 1;
}
finally {
    await Log.CloseAndFlushAsync();
}