using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Dapper.ETL.Orchestrator.Configuration;

public static class ObservabilityConfig {
    public static MeterProvider ConfigureOpenTelemetry(ILogger logger, string? otlpEndpoint = null) {
        var endpoint = otlpEndpoint ?? "http://localhost:4317"; // Aspire default OTLP/gRPC

        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("Dapper.ETL")
            .AddOtlpExporter(options => { options.Endpoint = new Uri(endpoint); })
            .Build();

        logger.LogInformation("OpenTelemetry OTLP exporter configured: {Endpoint}", endpoint);

        return meterProvider;
    }

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration config,
        ILogger logger) {
        var otlpEndpoint = config["OpenTelemetry:OtlpEndpoint"];

        MeterProvider meterProvider;
        try {
            meterProvider = ConfigureOpenTelemetry(logger, otlpEndpoint);
        }
        catch (Exception ex) {
            logger.LogWarning(ex,
                "Failed to configure OTLP exporter for endpoint {Endpoint}. Metrics export will be unavailable.",
                otlpEndpoint ?? "http://localhost:4317");

            // Fallback: build a provider without the OTLP exporter so the app continues running
            meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("Dapper.ETL")
                .Build();
        }

        services.AddSingleton(meterProvider);
        services.AddSingleton(new Meter("Dapper.ETL"));

        return services;
    }
}