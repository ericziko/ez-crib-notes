namespace Dapper.ETL.Orchestrator.Tests.Infrastructure;

using Dapper.ETL.Orchestrator.Infrastructure;
using Xunit;

public class SqlConnectionBuilderTests
{
    [Fact]
    public void Add_AccumulatesDescriptors()
    {
        var builder = new SqlConnectionBuilder();

        builder.Add("Source", "ConnectionStrings:Source", "SourceCred");
        builder.Add("Target", "ConnectionStrings:Target", "TargetCred");

        Assert.Equal(2, builder.Descriptors.Count);
    }

    [Fact]
    public void Add_StoresCorrectValues()
    {
        var builder = new SqlConnectionBuilder();

        builder.Add("Source", "ConnectionStrings:Source", "SourceCred");
        var d = builder.Descriptors[0];

        Assert.Equal("Source",                   d.ServiceKey);
        Assert.Equal("ConnectionStrings:Source", d.ConnectionStringKey);
        Assert.Equal("SourceCred",               d.CredentialKey);
    }

    [Fact]
    public void Add_IsChainable()
    {
        var builder = new SqlConnectionBuilder();

        var returned = builder.Add("A", "ConnectionStrings:A", "ACred");

        Assert.Same(builder, returned);
    }
}
