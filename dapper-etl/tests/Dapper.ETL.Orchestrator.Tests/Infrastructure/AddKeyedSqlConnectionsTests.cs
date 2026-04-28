using Dapper.ETL.Orchestrator.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dapper.ETL.Orchestrator.Tests.Infrastructure;

public class AddKeyedSqlConnectionsTests {
    private static IConfiguration Cfg(Dictionary<string, string?> values) {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void SingleConnection_ResolvesFromKeyedServices() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["SourceCred"] = "s3cr3t"
        });
        var services = new ServiceCollection();

        services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
            b.Add("Source", "ConnectionStrings:Source", "SourceCred"));

        var provider = services.BuildServiceProvider();
        var connStr = provider.GetRequiredKeyedService<string>("Source");

        Assert.NotNull(connStr);
        Assert.NotEmpty(connStr);
    }

    [Fact]
    public void MultipleConnections_AllResolveByKey() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["ConnectionStrings:Target"] = "Server=localhost;Database=Tgt;Encrypt=false",
            ["SourceCred"] = "s3cr3t",
            ["TargetCred"] = "t4rg3t"
        });
        var services = new ServiceCollection();

        services.AddKeyedSqlConnections(config, NullLogger.Instance, b => {
            b.Add("Source", "ConnectionStrings:Source", "SourceCred");
            b.Add("Target", "ConnectionStrings:Target", "TargetCred");
        });

        var provider = services.BuildServiceProvider();
        var sourceStr = provider.GetRequiredKeyedService<string>("Source");
        var targetStr = provider.GetRequiredKeyedService<string>("Target");

        Assert.NotEqual(sourceStr, targetStr);
        Assert.Contains("Src", sourceStr);
        Assert.Contains("Tgt", targetStr);
    }

    [Fact]
    public void MissingBaseConnectionString_ThrowsAtRegistrationTime() {
        var config = Cfg(new Dictionary<string, string?> { ["SourceCred"] = "s3cr3t" });
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
                b.Add("Source", "ConnectionStrings:Source", "SourceCred")));

        Assert.Contains("ConnectionStrings:Source", ex.Message);
    }

    [Fact]
    public void LogsConnectionProperties_DoesNotThrow() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["SourceCred"] = "s3cr3t"
        });
        var services = new ServiceCollection();

        var ex = Record.Exception(() =>
            services.AddKeyedSqlConnections(config, NullLogger.Instance, b =>
                b.Add("Source", "ConnectionStrings:Source", "SourceCred")));

        Assert.Null(ex);
    }

    [Fact]
    public void RegisteredConnectionString_DoesNotLogCredential() {
        var log = new List<string>();
        var logger = new RecordingLogger(log);

        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["SourceCred"] = "SuperSecret99"
        });
        var services = new ServiceCollection();

        services.AddKeyedSqlConnections(config, logger, b =>
            b.Add("Source", "ConnectionStrings:Source", "SourceCred"));

        Assert.DoesNotContain(log, m => m.Contains("SuperSecret99"));
    }
}