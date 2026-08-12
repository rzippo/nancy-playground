using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// Tests that the <c>out</c> option of plot commands gets the extension fitting the plot output,
/// so that the user does not have to worry about it.
/// </summary>
public class PlotOutPathParsing
{
    private static State StateWithFunction()
    {
        var state = new State();
        state.Add("f", Unipi.Nancy.Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(1, 3), "f"));
        return state;
    }

    [Theory]
    // a missing extension is added
    [InlineData("plot(f, out = \"chart\")", "chart.png")]
    [InlineData("plotTikz(f, out = \"chart\")", "chart.tikz")]
    // a compatible extension is left as is
    [InlineData("plot(f, out = \"chart.png\")", "chart.png")]
    [InlineData("plotTikz(f, out = \"chart.tikz\")", "chart.tikz")]
    [InlineData("plotTikz(f, out = \"chart.tex\")", "chart.tex")]
    // a wrong extension is replaced, rather than doubled
    [InlineData("plot(f, out = \"chart.tex\")", "chart.png")]
    [InlineData("plotTikz(f, out = \"chart.png\")", "chart.tikz")]
    // something that is not an extension is part of the name, hence preserved
    [InlineData("plot(f, out = \"rate-0.5\")", "rate-0.5.png")]
    [InlineData("plotTikz(f, out = \"rate-0.5\")", "rate-0.5.tikz")]
    public void OutOptionGetsExtensionOfItsPlotKind(string line, string expectedOutPath)
    {
        var statement = Statement.FromLine(line, StateWithFunction());

        var plot = Assert.IsAssignableFrom<PlotCommand>(statement);
        Assert.Equal(expectedOutPath, plot.Settings.OutPath);
    }

    [Fact]
    public void WithoutOutOption_OutPathIsEmpty()
    {
        var statement = Statement.FromLine("plotTikz(f)", StateWithFunction());

        var plot = Assert.IsType<PlotTikzCommand>(statement);
        Assert.Equal(string.Empty, plot.Settings.OutPath);
    }
}
