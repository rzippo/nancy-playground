using Unipi.Nancy.Expressions;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// Subtracting a scalar from a zero-latency <c>ratency</c> curve used
/// to crash as soon as the result was touched. Root cause was upstream, in
/// <c>Unipi.Nancy.NetworkCalculus.RaisedRateLatencyServiceCurve.PeriodStart</c>, whose
/// <c>(delay == 0 &amp;&amp; bufferShift &gt; 0)</c> guard only accounted for a positive shift and
/// fell back to <c>delay</c> (0) for a negative one, producing a segment whose end time equalled
/// its start time. <c>RateLatencyServiceCurve.VerticalShift</c> routed through that class
/// unconditionally, so both `-` and `+` a negative constant hit it; nonzero latency and other
/// curve shapes took a different, unaffected path.
/// Fixed upstream in <c>Unipi.Nancy</c> 1.4.5.
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
        // g(t) = f(t) + (-k) is the same shift as f(t) - k, and goes through the same
        // Addition/VerticalShift path with a negative bufferShift.
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

    // --- Regression guards for the neighbouring cases the bug report bisected as unaffected ---

    [Fact]
    public void NonZeroLatencyRatencyMinusScalarStillWorks()
    {
        var result = EvaluatePoint("y := ratency(2, 1) - 4\ny(10)", statementIndex: 1);
        Assert.Equal((Rational)14, result.Value); // 2 * (10 - 1) - 4
    }

    [Fact]
    public void OtherCurveShapeMinusScalarStillWorks()
    {
        var result = EvaluatePoint("z := bucket(2, 5) - 4\nz(10)", statementIndex: 1);
        Assert.Equal((Rational)21, result.Value);
    }

    [Fact]
    public void ZeroLatencyRatencyPlusPositiveConstantStillWorks()
    {
        // A positive shift is exactly the case PeriodStart's guard was written for.
        var result = EvaluatePoint("x := ratency(2, 0) + 4\nx(10)", statementIndex: 1);
        Assert.Equal((Rational)24, result.Value); // 2 * 10 + 4
    }

    [Fact]
    public void ZeroLatencyRatencyMinusZeroStillWorks()
    {
        var result = EvaluatePoint("x := ratency(2, 0) - 0\nx(10)", statementIndex: 1);
        Assert.Equal((Rational)20, result.Value); // shift == 0 is a documented no-op
    }
}
