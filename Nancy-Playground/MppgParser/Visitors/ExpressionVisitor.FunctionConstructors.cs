using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override (CurveExpression? Function, RationalExpression? Number) VisitRateLatency(Grammar.MppgParser.RateLatencyContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var (_, rateExp) = context.GetChild(2).Accept(this);
        var (_, latencyExp) = context.GetChild(4).Accept(this);

        if (rateExp is null || latencyExp is null)
            throw new Exception("Expected rate and latency expressions");

        var rate = rateExp.Compute();
        var latency = latencyExp.Compute();

        var curve = new RateLatencyServiceCurve(rate, latency);
        var curveExp = Expressions.FromCurve(curve, name: $"ratency_{{{rate}, {latency}}}");

        return (curveExp, null);
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitTokenBucket(Grammar.MppgParser.TokenBucketContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var (_, aExp) = context.GetChild(2).Accept(this);
        var (_, bExp) = context.GetChild(4).Accept(this);

        if (aExp is null || bExp is null)
            throw new Exception("Expected a and b expressions");

        var a = aExp.Compute();
        var b = bExp.Compute();

        var curve = new SigmaRhoArrivalCurve(b, a);
        var curveExp = Expressions.FromCurve(curve, name: $"bucket_{{{a}, {b}}}");

        return (curveExp, null);
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitAffineFunction(
        Grammar.MppgParser.AffineFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var (_, slopeExp) = context.GetChild(2).Accept(this);
        var (_, constantExp) = context.GetChild(4).Accept(this);

        if (slopeExp is null || constantExp is null)
            throw new Exception("Expected slope and constant expressions");

        var slope = slopeExp.Compute();
        var constant = constantExp.Compute();

        var curve = new Curve(
            new Sequence([
                new Point(0, constant),
                new Segment(0, 1, constant, slope)
            ]),
            0, 1, slope
        );
        var curveExp = Expressions.FromCurve(curve, name: $"affine_{{{slope}, {constant}}}");

        return (curveExp, null);
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitStairFunction(
        Grammar.MppgParser.StairFunctionContext context)
    {
        if (context.ChildCount != 8)
            throw new Exception("Expected 8 child expression");

        var (_, oExp) = context.GetChild(2).Accept(this);
        var (_, lExp) = context.GetChild(4).Accept(this);
        var (_, hExp) = context.GetChild(6).Accept(this);

        if (oExp is null || lExp is null || hExp is null)
            throw new Exception("Expected expressions for o, l and h");

        var o = oExp.Compute();
        var l = lExp.Compute();
        var h = hExp.Compute();

        var curve = new Curve(
            new Sequence([
                Point.Origin(),
                new Segment(0, l, h, 0)
            ]),
            0, l, h
        ).DelayBy(o);
        var curveExp = Expressions.FromCurve(curve, name: $"stair_{{{o}, {l}, {h}}}");

        return (curveExp, null);
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitZeroFunction(
        Grammar.MppgParser.ZeroFunctionContext context)
    {
        var curveExp = Expressions.FromCurve(Curve.Zero());
        return (curveExp, null);
    }
}