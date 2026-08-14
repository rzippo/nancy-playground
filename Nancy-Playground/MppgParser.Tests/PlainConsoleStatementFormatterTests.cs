using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class PlainConsoleStatementFormatterTests
{
    [Fact]
    public void PlotReportsItIsNotRendered()
    {
        var formatter = new PlainConsoleStatementFormatter();
        var statement = new PlotCommand();
        var output = new PlotOutput { StatementText = string.Empty, OutputText = string.Empty };

        var text = CaptureConsole(() => formatter.FormatStatementOutput(statement, output));

        Assert.Contains("Plots are not rendered in this output mode.", text);
        Assert.DoesNotContain("If you are reading this", text);
    }

    private static string CaptureConsole(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }
        return writer.ToString();
    }
}
