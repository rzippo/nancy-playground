using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The <c>upp</c> increment and period length take a number expression, as every constructor argument and endpoint does.
/// RTaW takes the same forms in the length field: a variable, a compound of these, or a parenthesised literal.
/// The increment is authoritative.
/// The length is informational: a declared one that does not match the sequence warns, and the sequence is what the curve uses.
/// </summary>
public class UppFieldsParsing
{
    private const string Period = "period([(0, 0)] ](0, 0) 0 (1, 0)[)";

    private static State StateWithNumbers() =>
        new(
            [
                ("v", Expressions.Expressions.FromRational(new Rational(2), "v")),
                ("hv", Expressions.Expressions.FromRational(new Rational(1, 2), "hv")),
                ("nv", Expressions.Expressions.FromRational(new Rational(-1, 2), "nv")),
            ]
        );

    private static CurveExpression Parsed(string mppg, State state) =>
        Assert.IsAssignableFrom<CurveExpression>(ExpressionParsing.Parse(mppg, state));

    [Theory]
    [InlineData("upp(UPPER, v)", 2)]
    [InlineData("upp(UPPER, v, 5)", 2)]
    [InlineData("upp(UPPER, hv, 5)", 1, 2)]
    [InlineData("upp(UPPER, -v, 5)", -2)]
    [InlineData("upp(UPPER, v+v, 5)", 4)]
    public void TheIncrementTakesAnExpression(string mppg, long expectedNumerator, long expectedDenominator = 1)
    {
        var expression = Parsed(mppg.Replace("UPPER", Period), StateWithNumbers());

        Assert.Equal(new Rational(expectedNumerator, expectedDenominator), expression.Value.PseudoPeriodHeight);
    }

    [Theory]
    [InlineData("upp(UPPER, 1, v)", 1)]
    [InlineData("upp(UPPER, 1, hv)", 1)]
    [InlineData("upp(UPPER, 1, nv)", 1)]
    [InlineData("upp(UPPER, 1, v+v)", 1)]
    [InlineData("upp(UPPER, 1, 1/2/3)", 1)]
    [InlineData("upp(UPPER, 1, (1/2))", 1)]
    [InlineData("upp(UPPER, 1, ((1/2)))", 1)]
    [InlineData("upp(UPPER, (1/2))", 1, 2)]
    [InlineData("upp(UPPER, ((1/2)))", 1, 2)]
    public void TheFieldsTakeTheFormsRtaWAccepts(string mppg, long expectedNumerator, long expectedDenominator = 1)
    {
        var expression = Parsed(mppg.Replace("UPPER", Period), StateWithNumbers());

        Assert.Equal(new Rational(expectedNumerator, expectedDenominator), expression.Value.PseudoPeriodHeight);
    }

    [Fact]
    public void ADeclaredLengthEntersNothing()
    {
        var declared = Parsed($"upp({Period}, 1, 99)", new State());
        var undeclared = Parsed($"upp({Period}, 1)", new State());

        Assert.True(Unipi.Nancy.MinPlusAlgebra.Curve.Equivalent(declared.Value, undeclared.Value));
    }

    [Fact]
    public void ADeclaredLengthThatDoesNotMatchTheSequenceWarns()
    {
        var state = StateWithNumbers();
        var statement = Statement.FromLine($"x := upp({Period}, 1, 99)", state);
        var output = statement.ExecuteToFormattable(state);

        var warning = Assert.Single(output.Warnings);
        Assert.Contains("99", warning);
        Assert.Contains("1", warning);
    }

    [Fact]
    public void ADeclaredLengthCarriedByAVariableWarnsOnItsValue()
    {
        var state = StateWithNumbers();
        var statement = Statement.FromLine($"x := upp({Period}, 1, v)", state);
        var output = statement.ExecuteToFormattable(state);

        var warning = Assert.Single(output.Warnings);
        Assert.Contains("2", warning);
        Assert.Contains("1", warning);
    }

    [Theory]
    [InlineData($"upp({Period}, 1)")]
    [InlineData($"upp({Period}, 1, 1)")]
    public void ALengthThatMatchesTheSequenceWarnsNothing(string mppg)
    {
        var statement = Statement.FromLine($"x := {mppg}", new State());
        var output = statement.ExecuteToFormattable(new State());

        Assert.Empty(output.Warnings);
    }

    [Fact]
    public void AFunctionIsRejectedAsAField()
    {
        // a number expression position, as every constructor argument is: the parser rejects a function there
        var exception = Assert.ThrowsAny<Exception>(
            () => ExpressionParsing.Parse($"upp({Period}, f, 5)", StateWithFunction()));

        Assert.Contains("'f' is a function, which cannot stand here", exception.Message);
    }

    private static State StateWithFunction() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(new Unipi.Nancy.NetworkCalculus.RateLatencyServiceCurve(2, 5), "f")),
            ]
        );
}
