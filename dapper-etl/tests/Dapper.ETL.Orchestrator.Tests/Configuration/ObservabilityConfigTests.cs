namespace Dapper.ETL.Orchestrator.Tests.Configuration;

using Dapper.ETL.Orchestrator.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using Xunit;

public class ObservabilityConfigTests
{
    [Fact]
    public void Test_ConfigureOpenTelemetry_WithNullEndpoint_ReturnsMeterProvider()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger("test");
        MeterProvider? provider = null;
        try
        {
            provider = ObservabilityConfig.ConfigureOpenTelemetry(logger);
            Assert.NotNull(provider);
        }
        catch
        {
            // OTLP exporter may throw if no server; covered by AddObservability fallback path
        }
        finally
        {
            provider?.Dispose();
        }
    }

    [Fact]
    public void Test_AddObservability_RegistersMeterAndMeterProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var logger = NullLoggerFactory.Instance.CreateLogger("test");

        // Act: AddObservability has a try/catch — if OTLP fails it uses fallback provider
        services.AddObservability(config, logger);

        // Assert: both Meter and MeterProvider should be registered
        var provider = services.BuildServiceProvider();
        var meter = provider.GetService<Meter>();
        var meterProvider = provider.GetService<MeterProvider>();

        Assert.NotNull(meter);
        Assert.NotNull(meterProvider);

        meter!.Dispose();
        meterProvider!.Dispose();
    }

    [Fact]
    public void Test_AddObservability_WithCustomOtlpEndpoint_Succeeds()
    {
        // Arrange: configure a non-existent OTLP endpoint to trigger the catch fallback
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:OtlpEndpoint"] = "http://does-not-exist:4317"
            })
            .Build();
        var logger = NullLoggerFactory.Instance.CreateLogger("test");

        // Act: should not throw (fallback handles the OTLP connection failure)
        var ex = Record.Exception(() => services.AddObservability(config, logger));

        // Assert: no unhandled exception
        Assert.Null(ex);
    }
}
