using Unipi.Nancy.Expressions;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The pseudo-period fields of <c>upp</c> and the plot intervals take a literal, rather than the
/// number expression that the surrounding constructs take.
/// These cover the fraction form of that literal, which is how Nancy writes a rational that is
/// neither an integer nor a decimal, hence how it comes back in a script printed from a curve.
/// </summary>
public class RationalLiteralParsing
{
    // a flat pseudo-period of length 1, starting at 0 as it must with no transient part,
    // so that the field under test is the only source of the pseudo-period height
    private const string Period = "period([(0, 0)] ](0, 0) 0 (1, 0)[)";

    private static State StateWithFunction() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(2, 5), "f")),
            ]
        );

    public static IEnumerable<object[]> UppIncrementTestCases =>
        new List<(string mppg, Rational expected)>
        {
            // the forms that were already accepted
            ($"upp({Period}, 1, 2)", new Rational(1)),
            ($"upp({Period}, 0.5, 0.5)", new Rational(1, 2)),
            ($"upp({Period}, -1)", new Rational(-1)),
            ($"upp({Period}, +inf)", Rational.PlusInfinity),
            // the height as a fraction
            ($"upp({Period}, 1/2, 2)", new Rational(1, 2)),
            ($"upp({Period}, 3/2)", new Rational(3, 2)),
            ($"upp({Period}, -1/2, 2)", new Rational(-1, 2)),
            ($"upp({Period}, +1/2, 2)", new Rational(1, 2)),
            // the period length as a fraction, which is informational: the height is unaffected
            ($"upp({Period}, 1, 1/2)", new Rational(1)),
            ($"upp({Period}, 1/2, 5/2)", new Rational(1, 2)),
            // decimals divide too, the same way they do in a segment
            ($"upp({Period}, 0.5/2, 2)", new Rational(1, 4)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(UppIncrementTestCases))]
    public void UppPseudoPeriodHeightAcceptsARationalLiteral(string mppg, Rational expected)
    {
        var expression = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(mppg, new State()));

        Assert.Equal(expected, expression.Value.PseudoPeriodHeight);
    }

    /// <summary>
    /// The declared period length is informational, here as everywhere else: the length actually
    /// used is the one of the period sequence, whatever the literal says.
    /// </summary>
    [Fact]
    public void UppPeriodLengthLiteralStaysInformational()
    {
        var expression = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse($"upp({Period}, 1/2, 5/2)", new State()));

        Assert.Equal(new Rational(1), expression.Value.PseudoPeriodLength);
    }

    public static IEnumerable<object[]> PlotIntervalTestCases =>
        new List<(string mppg, (Rational Left, Rational Right) expected)>
        {
            // the forms that were already accepted
            ("plot(f, xlim=[0, 10])", (new Rational(0), new Rational(10))),
            ("plot(f, xlim=[-0.5, 10])", (new Rational(-1, 2), new Rational(10))),
            // and the fraction form
            ("plot(f, xlim=[1/2, 10])", (new Rational(1, 2), new Rational(10))),
            ("plot(f, xlim=[-1/2, 21/2])", (new Rational(-1, 2), new Rational(21, 2))),
            ("plotTikz(f, xlim=[1/2, 10])", (new Rational(1, 2), new Rational(10))),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(PlotIntervalTestCases))]
    public void PlotXLimitAcceptsARationalLiteral(string line, (Rational Left, Rational Right) expected)
    {
        var statement = Statement.FromLine(line, StateWithFunction());

        var plot = Assert.IsAssignableFrom<PlotCommand>(statement);
        Assert.Equal(expected, plot.Settings.XLimit);
    }

    [Theory]
    [InlineData("plot(f, ylim=[1/2, 10])")]
    [InlineData("plotTikz(f, ylim=[1/2, 10])")]
    public void PlotYLimitAcceptsARationalLiteral(string line)
    {
        var statement = Statement.FromLine(line, StateWithFunction());

        var plot = Assert.IsAssignableFrom<PlotCommand>(statement);
        Assert.Equal((new Rational(1, 2), new Rational(10)), plot.Settings.YLimit);
    }

    [Fact]
    public void AnUppIncrementWithZeroDenominatorIsRejected()
    {
        var exception = Assert.ThrowsAny<Exception>(
            () => ExpressionParsing.Parse($"upp({Period}, 1/0)", new State()));

        Assert.Contains("denominator is zero", exception.Message);
    }

    [Fact]
    public void APlotIntervalWithZeroDenominatorIsRejected()
    {
        var exception = Assert.ThrowsAny<Exception>(
            () => Statement.FromLine("plot(f, xlim=[1/0, 10])", StateWithFunction()));

        Assert.Contains("denominator is zero", exception.Message);
    }
}
