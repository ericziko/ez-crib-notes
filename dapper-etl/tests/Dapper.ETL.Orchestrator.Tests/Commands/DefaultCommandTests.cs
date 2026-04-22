using Dapper.ETL.Orchestrator.Commands;
using Dapper.ETL.Orchestrator.Tests.Fixtures;

namespace Dapper.ETL.Orchestrator.Tests.Commands;

[Collection("SharedSqlServer collection")]
public class DefaultCommandTests {
    private readonly SharedSqlServerFixture _fixture;

    public DefaultCommandTests(SharedSqlServerFixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public async Task Execute_ReturnsZero() {
        var command = new DefaultCommand(CommandTestFixtures.BuildDataService(_fixture));
        var context = CommandTestFixtures.CreateContext("default");

        var result = command.Execute(context);

        Assert.Equal(0, result);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Execute_RendersCommandTable() {
        var command = new DefaultCommand(CommandTestFixtures.BuildDataService(_fixture));
        var context = CommandTestFixtures.CreateContext("default");

        var output = CommandTestFixtures.CaptureAnsiOutput(() => command.Execute(context));

        await Verify(output).UseDirectory("Snapshots");
    }
}