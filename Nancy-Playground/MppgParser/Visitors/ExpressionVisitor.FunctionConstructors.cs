using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitRateLatency(Grammar.MppgParser.RateLatencyContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var irateExp = context.GetChild(2).Accept(this);
        var ilatencyExp = context.GetChild(4).Accept(this);

        if (irateExp is not RationalExpression rateExp || ilatencyExp is not RationalExpression latencyExp)
            throw new Exception("Expected rate and latency expressions");

        var rate = rateExp.Compute();
        var latency = latencyExp.Compute();

        var curve = new RateLatencyServiceCurve(rate, latency);
        var curveExp = Expressions.FromCurve(curve, name: $"ratency_{{{rate}, {latency}}}");

        return curveExp;
    }

    public override IExpression VisitTokenBucket(Grammar.MppgParser.TokenBucketContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var iaExp = context.GetChild(2).Accept(this);
        var ibExp = context.GetChild(4).Accept(this);

        if (iaExp is not RationalExpression aExp || ibExp is not RationalExpression bExp)
            throw new Exception("Expected a and b expressions");

        var a = aExp.Compute();
        var b = bExp.Compute();

        var curve = new SigmaRhoArrivalCurve(b, a);
        var curveExp = Expressions.FromCurve(curve, name: $"bucket_{{{a}, {b}}}");

        return curveExp;
    }

    public override IExpression VisitAffineFunction(
        Grammar.MppgParser.AffineFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var islopeExp = context.GetChild(2).Accept(this);
        var iconstantExp = context.GetChild(4).Accept(this);

        if (islopeExp is not RationalExpression slopeExp || iconstantExp is not RationalExpression constantExp)
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

        return curveExp;
    }

    public override IExpression VisitStairFunction(
        Grammar.MppgParser.StairFunctionContext context)
    {
        if (context.ChildCount != 8)
            throw new Exception("Expected 8 child expression");

        var ioExp = context.GetChild(2).Accept(this);
        var ilExp = context.GetChild(4).Accept(this);
        var ihExp = context.GetChild(6).Accept(this);

        if (ioExp is not RationalExpression oExp || ilExp is not RationalExpression lExp || ihExp is not RationalExpression hExp)
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

        return curveExp;
    }

    public override IExpression VisitZeroFunction(
        Grammar.MppgParser.ZeroFunctionContext context)
    {
        var curveExp = Expressions.FromCurve(Curve.Zero());
        return curveExp;
    }
}