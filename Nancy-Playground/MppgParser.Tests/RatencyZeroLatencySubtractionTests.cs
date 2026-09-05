using Unipi.Nancy.Expressions;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// Subtracting a scalar from a zero-latency <c>ratency</c> curve used to crash as soon as the result was touched, in <c>Unipi.Nancy</c> up to 1.4.4.
/// <c>RaisedRateLatencyServiceCurve.PeriodStart</c> accounted only for a positive shift and fell back to the latency for a negative one, giving a segment whose end time equalled its start time.
/// Both <c>-</c> and <c>+</c> a negative constant reached it, while a nonzero latency and other curve shapes did not.
/// </summary>
public class RatencyZeroLatencySubtractionTests
{
    public static IEnumerable<object[]> RateAndShiftCases =>
        new List<object[]>
        {
            new object[] { (Rational)2, (Rational)4 }, // as filed
            new object[] { (Rational)2, (Rational)1 }, // bisected: not specific to the constant's size
            new object[] { (Rational)5, (Rational)10 },
            new object[] { (Rational)1, (Rational)1 },
        };

    private static RationalExpression EvaluatePoint(string program, int statementIndex)
    {
        var p = Program.FromText(program);
        Assert.Empty(p.Errors);
        for (var i = 0; i < statementIndex; i++)
            p.Statements[i].ExecuteToFormattable(p.ProgramContext.State);

        var output = (ExpressionOutput)p.Statements[statementIndex].ExecuteToFormattable(p.ProgramContext.State);
        return Assert.IsAssignableFrom<RationalExpression>(output.Expression);
    }

    [Theory]
    [MemberData(nameof(RateAndShiftCases))]
    public void ZeroLatencyRatencyMinusScalarEvaluatesToTheShiftedLine(Rational rate, Rational shift)
    {
        foreach (var t in new Rational[] { 0, 1, 10 })
        {
            var result = EvaluatePoint($"x := ratency({rate}, 0) - {shift}\nx({t})", statementIndex: 1);
            Assert.Equal(rate * t - shift, result.Value);
        }
    }

    [Theory]
    [MemberData(nameof(RateAndShiftCases))]
    public void ZeroLatencyRatencyPlusNegativeConstantEvaluatesToTheShiftedLine(Rational rate, Rational shift)
    {
        // adding a negative constant is the same shift, over the same path
        foreach (var t in new Rational[] { 0, 1, 10 })
        {
            var result = EvaluatePoint($"x := ratency({rate}, 0) + ({-shift})\nx({t})", statementIndex: 1);
            Assert.Equal(rate * t - shift, result.Value);
        }
    }

    [Theory]
    [MemberData(nameof(RateAndShiftCases))]
    public void ZeroLatencyRatencyMinusScalarPrintsWithoutCrashing(Rational rate, Rational shift)
    {
        var p = Program.FromText($"x := ratency({rate}, 0) - {shift}\nx");
        Assert.Empty(p.Errors);
        p.Statements[0].ExecuteToFormattable(p.ProgramContext.State);

        var output = p.Statements[1].ExecuteToFormattable(p.ProgramContext.State);

        Assert.DoesNotContain("Segment end time", output.OutputText);
    }

    [Theory]
    [MemberData(nameof(RateAndShiftCases))]
    public void ZeroLatencyRatencyMinusScalarInsideNnupclosureDoesNotCrash(Rational rate, Rational shift)
    {
        var p = Program.FromText($"x := nnupclosure(ratency({rate}, 0) - {shift})\nx");
        Assert.Empty(p.Errors);
        p.Statements[0].ExecuteToFormattable(p.ProgramContext.State);

        var output = p.Statements[1].ExecuteToFormattable(p.ProgramContext.State);

        Assert.DoesNotContain("Segment end time", output.OutputText);
    }

    /// <summary>
    /// The shifts either side of the one that broke, each with its value at $t = 10$:
    /// a nonzero latency, a curve of another shape, a positive shift, and a shift of zero.
    /// </summary>
    public static IEnumerable<object[]> NeighbouringShiftCases =>
        new List<object[]>
        {
            new object[] { "y := ratency(2, 1) - 4\ny(10)", (Rational)14 },  // 2 * (10 - 1) - 4
            new object[] { "z := bucket(2, 5) - 4\nz(10)", (Rational)21 },   // 5 + 2 * 10 - 4
            new object[] { "x := ratency(2, 0) + 4\nx(10)", (Rational)24 },  // 2 * 10 + 4, the sign PeriodStart's guard was written for
            new object[] { "x := ratency(2, 0) - 0\nx(10)", (Rational)20 },  // a shift of zero returns the curve itself
        };

    [Theory]
    [MemberData(nameof(NeighbouringShiftCases))]
    public void AShiftBesideTheBrokenOneEvaluatesToTheShiftedCurve(string program, Rational expected)
    {
        var result = EvaluatePoint(program, statementIndex: 1);

        Assert.Equal(expected, result.Value);
    }
}
