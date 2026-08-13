using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// Neither grouping errors, so the parser warns on exactly the shapes where we and RTaW disagree.
/// See "Scalar operands of the mixed operators" in syntax.md.
/// </summary>
public class ScalarDivisionGroupingWarning
{
    private static State StateWithVariables() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(2, 5), "f")),
                ("g", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(1, 1), "g")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(new Rational(2), "x")),
                ("y", Expressions.Expressions.FromRational(new Rational(3), "y")),
            ]
        );

    [Theory]
    // the divisor starts with a number and more scalar factors follow it
    [InlineData("a := f / 1/2")]
    [InlineData("a := f / 1 / 2")]
    [InlineData("a := f / 1/2/3")]
    [InlineData("a := f / 1 * y")]
    [InlineData("a := f / -1 / y")]
    [InlineData("a := f / 0.5/2")]
    [InlineData("a := f / 1/y")]
    public void AmbiguousScalarDivisionIsReported(string line)
    {
        var statement = Statement.FromLine(line, StateWithVariables());

        var warning = Assert.Single(statement.Warnings);
        Assert.Contains("Parenthesise the divisor", warning);
    }

    [Theory]
    // one factor only, so there is nothing to regroup
    [InlineData("a := f / 2")]
    [InlineData("a := f / x")]
    // the divisor starts with a variable or a sampled value, where RTaW folds left exactly as we do
    [InlineData("a := f / x / y")]
    [InlineData("a := f / x * y")]
    [InlineData("a := f / x/2")]
    [InlineData("a := f / g(2)/2")]
    // a multiplication first, where the two groupings agree by associativity
    [InlineData("a := f * x / y")]
    [InlineData("a := f * 1/2")]
    [InlineData("a := f * x * y")]
    // the divisor is already parenthesised, so it says what it means
    [InlineData("a := f / (1/2)")]
    [InlineData("a := f / (1/2) / y")]
    // a deconvolution, not a scalar division
    [InlineData("a := f / g / x")]
    // the next operator cannot join a scalar chain
    [InlineData("a := f / 2 *_ g")]
    // no curve involved at all
    [InlineData("a := x / 1/2")]
    public void UnambiguousScalarDivisionIsNotReported(string line)
    {
        var statement = Statement.FromLine(line, StateWithVariables());

        Assert.Empty(statement.Warnings);
    }

    /// <summary>
    /// The warning names the chain it is about, so a line carrying several is still readable.
    /// </summary>
    [Fact]
    public void TheWarningQuotesTheOffendingChain()
    {
        var statement = Statement.FromLine("a := f / 1/2", StateWithVariables());

        var warning = Assert.Single(statement.Warnings);
        Assert.Contains("f / 1 / 2", warning);
    }

    /// <summary>
    /// A diagnostic, not a rejection: the statement still parses and still folds left.
    /// </summary>
    [Fact]
    public void TheWarningDoesNotChangeTheValue()
    {
        var state = StateWithVariables();

        var withChain = ExpressionParsing.Parse("f / 1/2", state);
        var foldedLeft = ExpressionParsing.Parse("(f / 1) / 2", state);

        var actual = Assert.IsAssignableFrom<CurveExpression>(withChain).Compute();
        var expected = Assert.IsAssignableFrom<CurveExpression>(foldedLeft).Compute();
        Assert.True(Curve.Equivalent(expected, actual));
    }
}
