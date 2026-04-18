namespace Dapper.ETL.Orchestrator.Tests.Fixtures;

using Dapper.ETL.Orchestrator.Services;
using Moq;
using Spectre.Console;
using Spectre.Console.Cli;

public static class CommandTestFixtures
{
    public static DataService BuildDataService(SharedSqlServerFixture fixture)
        => new(
            fixture.GetConnectionString("TestDbSource"),
            fixture.GetConnectionString("TestDbTarget"),
            fixture.GetConnectionString("EtlLogs"));

    public static CommandContext CreateContext(string name)
    {
        var arguments = Array.Empty<string>();
        var remaining = new Mock<IRemainingArguments>().Object;
        return new CommandContext(arguments, remaining, name, null);
    }

    public static string CaptureAnsiOutput(Func<int> action)
    {
        var writer = new StringWriter();
        var prev = AnsiConsole.Console;
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            ColorSystem = ColorSystemSupport.NoColors,
            Ansi = AnsiSupport.No
        });
        try
        {
            action();
        }
        finally
        {
            AnsiConsole.Console = prev;
        }
        return writer.ToString();
    }
}
