using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Numerics;

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

    public override IExpression VisitStepFunction(
        Grammar.MppgParser.StepFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ioExp = context.GetChild(2).Accept(this);
        var ihExp = context.GetChild(4).Accept(this);

        if (ioExp is not RationalExpression oExp || ihExp is not RationalExpression hExp)
            throw new Exception("Expected expressions for o, l and h");

        var o = oExp.Compute();
        var h = hExp.Compute();

        var curve = new StepCurve(h, o);
        var curveExp = Expressions.FromCurve(curve, name: $"step_{{{o}, {h}}}");

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

    public override IExpression VisitDelayFunction(
        Grammar.MppgParser.DelayFunctionContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var idExp = context.GetChild(2).Accept(this);

        if (idExp is not RationalExpression dExp)
            throw new Exception("Expected expression for d");

        var d = dExp.Compute();

        var curve = new DelayServiceCurve(d);
        var curveExp = curve.ToExpression();

        return curveExp;
    }
    
    public override IExpression VisitZeroFunction(
        Grammar.MppgParser.ZeroFunctionContext context)
    {
        var curveExp = Expressions.FromCurve(Curve.Zero());
        return curveExp;
    }

    public override IExpression VisitEpsilonFunction(
        Grammar.MppgParser.EpsilonFunctionContext context)
    {
        var curveExp = Expressions.FromCurve(Curve.PlusInfinite());
        return curveExp;
    }

    public override IExpression VisitUltimatelyPseudoPeriodicFunction(Grammar.MppgParser.UltimatelyPseudoPeriodicFunctionContext context)
    {
        var uppText = context.GetJoinedText();

        var transientElements = Enumerable.Empty<Element>();
        var elementsVisitor = new ElementsVisitor();
        
        var transientContext = context.GetChild<Grammar.MppgParser.UppTransientPartContext>(0);
        if (transientContext is not null)
        {
            var transientSequenceContext = transientContext.GetChild<Grammar.MppgParser.SequenceContext>(0);
            var parsedTransientElements = transientSequenceContext.Accept(elementsVisitor);
            transientElements = transientElements.Concat(parsedTransientElements);
        }
        var transientElementsList = transientElements.ToList();
        if(transientElementsList.Any(e => e.StartTime.IsInfinite || e.EndTime.IsInfinite))
            throw new InvalidOperationException($"Elements with infinite time are not supported in UPP functions: {uppText}");

        var periodContext = context.GetChild<Grammar.MppgParser.UppPeriodicPartContext>(0);
        var periodSequenceContext = periodContext.GetChild<Grammar.MppgParser.SequenceContext>(0);
        var periodElements = periodSequenceContext.Accept(elementsVisitor);
        var periodElementsList = periodElements.ToList();
        if(periodElementsList.Any(e => e.StartTime.IsInfinite || e.EndTime.IsInfinite))
            throw new InvalidOperationException($"Elements with infinite time are not supported in UPP functions: {uppText}");

        var periodSequence = new Sequence(periodElements);

        var t = periodSequence.DefinedFrom;
        var d = periodSequence.DefinedUntil - periodSequence.DefinedFrom;
        Rational c = 0;

        var incrementContext = context.GetChild<Grammar.MppgParser.IncrementContext>(0);
        if (incrementContext is not null)
        {
            var incrementLiteralContext = incrementContext.GetChild(1);
            var numberLiteralVisitor = new NumberLiteralVisitor();
            var increment = incrementLiteralContext.Accept(numberLiteralVisitor);
            c = increment;
        }

        IEnumerable<Element> allElements;
        if (periodSequence is { IsLeftOpen: true, IsRightClosed: true })
        {
            // problem: period must be nudged so that it is actually left-closed, right-open
            // we do it using the first segment
            var firstSegment = (Segment) periodSequence.Elements[0];
            var firstSegmentShifted = firstSegment
                .HorizontalShift(d)
                .VerticalShift(c);
            allElements = transientElementsList
                .Concat(periodSequence.Elements)
                .Append(firstSegmentShifted);
            t += firstSegment.Length;
        }
        else if (periodSequence is { IsLeftClosed: true, IsRightOpen: true })
        {
            allElements = transientElementsList
                .Concat(periodSequence.Elements);
        }
        else
        {
            // defensive check
            throw new InvalidOperationException($"This period sequence cannot work: {uppText}");
        }

        var sequence = new Sequence(allElements);
        var curve = new Curve(
            sequence,
            t, d, c
        );
        return curve
            .ToExpression("");
    }

    public override IExpression VisitUltimatelyAffineFunction(Grammar.MppgParser.UltimatelyAffineFunctionContext context)
    {
        var elementsVisitor = new ElementsVisitor();

        var sequenceContext = context.GetChild<Grammar.MppgParser.SequenceContext>(0);
        var elements = sequenceContext.Accept(elementsVisitor);
        var sequence = new Sequence(elements);

        Rational t, d, c;
        if (sequence is not { Elements: { Count: >= 2 }})
            throw new InvalidOperationException("This UAF sequence cannot work");

        if (sequence is { IsRightOpen: true })
        {
            var lastPoint = (Point)sequence.Elements[^2];
            var lastSegment = (Segment)sequence.Elements[^1];
            t = lastPoint.Time;
            d = lastSegment.Length;
            c = lastSegment.LeftLimitAtEndTime - lastSegment.RightLimitAtStartTime;    
        }
        else
        {
            var lastPoint = (Point)sequence.Elements[^1];
            var lastSegment = (Segment)sequence.Elements[^2];
            t = lastPoint.Time;
            d = lastSegment.Length;
            c = lastSegment.LeftLimitAtEndTime - lastSegment.RightLimitAtStartTime;
            var lastSegmentShifted = lastSegment
                .HorizontalShift(d)
                .VerticalShift(c);
            sequence = new Sequence(sequence.Elements.Append(lastSegmentShifted));
        }

        var curve = new Curve(
            sequence,
            t, d, c
        );

        if(curve is not {IsUltimatelyAffine: true})
            throw new InvalidOperationException("This curve is not UA");

        return curve
            .ToExpression("");
    }
}