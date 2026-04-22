using Dapper.ETL.Orchestrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dapper.ETL.Orchestrator.Tests.Infrastructure;

public class SqlConnectionBuilderTests {
    [Fact]
    public void Add_AccumulatesDescriptors() {
        var builder = new SqlConnectionBuilder();

        builder.Add("Source", "ConnectionStrings:Source", "SourceCred");
        builder.Add("Target", "ConnectionStrings:Target", "TargetCred");

        Assert.Equal(2, builder.Descriptors.Count);
    }

    [Fact]
    public void Add_StoresCorrectValues() {
        var builder = new SqlConnectionBuilder();

        builder.Add("Source", "ConnectionStrings:Source", "SourceCred");
        var d = builder.Descriptors[0];

        Assert.Equal("Source", d.ServiceKey);
        Assert.Equal("ConnectionStrings:Source", d.ConnectionStringKey);
        Assert.Equal("SourceCred", d.CredentialKey);
    }

    [Fact]
    public void Add_IsChainable() {
        var builder = new SqlConnectionBuilder();

        var returned = builder.Add("A", "ConnectionStrings:A", "ACred");

        Assert.Same(builder, returned);
    }
}

public class AssembleConnectionStringTests {
    private static IConfiguration Cfg(Dictionary<string, string?> values) {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void WithCredential_CredentialIsApplied() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["MyCred"] = "s3cr3t"
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MyCred");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.False(builder.IntegratedSecurity);
    }

    [Fact]
    public void WithoutCredential_UsesIntegratedSecurity() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false"
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MissingCred");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.True(builder.IntegratedSecurity);
    }

    [Fact]
    public void MissingBaseConnectionString_ThrowsInvalidOperationException() {
        var config = Cfg(new Dictionary<string, string?> { ["MyCred"] = "s3cr3t" });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SqlConnectionExtensions.AssembleConnectionString(
                config, "ConnectionStrings:Source", "MyCred"));

        Assert.Contains("ConnectionStrings:Source", ex.Message);
    }

    [Fact]
    public void Result_IsValidSqlConnectionString() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=Src;Encrypt=false",
            ["MyCred"] = "s3cr3t"
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MyCred");

        var ex = Record.Exception(() => new SqlConnectionStringBuilder(result));
        Assert.Null(ex);
    }

    [Fact]
    public void Result_ContainsExpectedDatabase() {
        var config = Cfg(new Dictionary<string, string?> {
            ["ConnectionStrings:Source"] = "Server=localhost;Database=MyDb;Encrypt=false",
            ["MyCred"] = "s3cr3t"
        });

        var result = SqlConnectionExtensions.AssembleConnectionString(
            config, "ConnectionStrings:Source", "MyCred");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal("MyDb", builder.InitialCatalog);
    }
}

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

file sealed class RecordingLogger(List<string> messages) : ILogger {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
        return null;
    }
    public bool IsEnabled(LogLevel logLevel) {
        return true;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter) {
        messages.Add(formatter(state, exception));
    }
}