using Dapper.ETL.Orchestrator.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Dapper.ETL.Orchestrator.Tests.Infrastructure;

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