using Unipi.Nancy.Expressions;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// A plot interval bound (<c>xlim</c>/<c>ylim</c>) takes a full scalar expression, not only a literal:
/// RTaW accepts a bare or signed variable there, including one carrying a fraction (<c>xlim=[v, w]</c>).
/// It rejects only a compound expression like <c>(1+1)</c> or a parenthesised literal like <c>(1/2)</c>, which nancy-playground accepts as an extension.
/// </summary>
public class PlotIntervalExpressionParsing
{
    private static State StateWithVariables() =>
        new(
            [("f", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(2, 5), "f"))],
            [
                ("v", Expressions.Expressions.FromRational(new Rational(3), "v")),
                ("hv", Expressions.Expressions.FromRational(new Rational(1, 2), "hv")),
                ("nv", Expressions.Expressions.FromRational(new Rational(-1, 2), "nv")),
            ]
        );

    private static (Rational Left, Rational Right) ResolvedXLimit(string line)
    {
        var state = StateWithVariables();
        var plot = Assert.IsAssignableFrom<PlotCommand>(Statement.FromLine(line, state));
        var output = Assert.IsType<PlotOutput>(plot.ExecuteToFormattable(state));
        Assert.NotNull(output.XLimit);
        return output.XLimit!.Value;
    }

    [Fact]
    public void APlainVariableIsAcceptedAsAPlotIntervalBound() =>
        Assert.Equal((new Rational(3), new Rational(10)), ResolvedXLimit("plot(f, xlim=[v, 10])"));

    [Fact]
    public void AVariableIsAcceptedAsEitherBound() =>
        Assert.Equal((new Rational(0), new Rational(3)), ResolvedXLimit("plot(f, xlim=[0, v])"));

    [Fact]
    public void ASignedVariableIsAccepted() =>
        Assert.Equal((new Rational(-3), new Rational(10)), ResolvedXLimit("plot(f, xlim=[-v, 10])"));

    [Fact]
    public void AVariableCarryingAFractionIsAccepted() =>
        Assert.Equal((new Rational(1, 2), new Rational(10)), ResolvedXLimit("plot(f, xlim=[hv, 10])"));

    [Fact]
    public void AVariableCarryingANegativeFractionIsAccepted() =>
        Assert.Equal((new Rational(-1, 2), new Rational(10)), ResolvedXLimit("plot(f, xlim=[nv, 10])"));

    [Fact]
    public void BothBoundsCanBeVariables() =>
        Assert.Equal((new Rational(-1, 2), new Rational(3)), ResolvedXLimit("plot(f, xlim=[nv, v])"));

    [Fact]
    public void YLimitAlsoAcceptsAVariable()
    {
        var state = StateWithVariables();
        var plot = Assert.IsAssignableFrom<PlotCommand>(Statement.FromLine("plot(f, ylim=[v, 10])", state));
        var output = Assert.IsType<PlotOutput>(plot.ExecuteToFormattable(state));

        Assert.Equal((new Rational(3), new Rational(10)), output.YLimit);
    }

    [Fact]
    public void APlotIntervalStillAcceptsACompoundExpression()
    {
        // an extension over RTaW, which rejects xlim=[1+1, 10]; nancy-playground accepts it since a numeric position takes the full expression grammar once it takes one at all
        Assert.Equal((new Rational(2), new Rational(10)), ResolvedXLimit("plot(f, xlim=[1+1, 10])"));
    }

    [Theory]
    [InlineData("plot(f, xlim=[f, 10])")]
    [InlineData("plot(f, ylim=[0, f])")]
    public void APlotIntervalBoundRejectsAFunction(string line)
    {
        var state = StateWithVariables();
        var plot = Assert.IsAssignableFrom<PlotCommand>(Statement.FromLine(line, state));

        var exception = Assert.ThrowsAny<Exception>(() => plot.ExecuteToFormattable(state));

        Assert.Contains("not functions", exception.Message);
    }
}
