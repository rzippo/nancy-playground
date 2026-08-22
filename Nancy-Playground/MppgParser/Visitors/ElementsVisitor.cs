using Antlr4.Runtime;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// In MPPG, each end of a segment may be either inclusive or exclusive.
/// So, rather than just a <see cref="Segment"/>, they are a sequence of <see cref="Element"/>s, including <see cref="Point"/>s as well.
/// </summary>
public class ElementsVisitor : MppgBaseVisitor<IEnumerable<Element>>
{
    private readonly State _state;
    private readonly bool _normalizeInfiniteRightEndpoint;

    /// <summary>
    /// A visitor resolving coordinates against <paramref name="state"/>.
    /// <paramref name="normalizeInfiniteRightEndpoint"/> closes a segment that runs to infinity, which Nancy needs to build the curve.
    /// </summary>
    public ElementsVisitor(State? state = null, bool normalizeInfiniteRightEndpoint = false)
    {
        _state = state ?? new State();
        _normalizeInfiniteRightEndpoint = normalizeInfiniteRightEndpoint;
    }

    /// <summary>
    /// Reads a sequence, i.e. the elements it is written as.
    /// </summary>
    public override IEnumerable<Element> VisitSequence(Unipi.MppgParser.Grammar.MppgParser.SequenceContext context)
    {
        var elements = Enumerable.Empty<Element>();
        for (int i = 0; i < context.ChildCount; i++)
        {
            var elementContext = context.GetChild(i);
            var elementsParsed = elementContext.Accept(this);
            elements = elements.Concat(elementsParsed);
        }
        return elements;
    }

    /// <summary>
    /// Reads one element, which is a point or a segment.
    /// </summary>
    public override IEnumerable<Element> VisitElement(Unipi.MppgParser.Grammar.MppgParser.ElementContext context)
    {
        if (context.ChildCount != 1)
            throw new Exception("Expected 1 child expression");

        var childContext = context.GetChild(0);
        var elements = childContext.Accept(this);
        return elements;
    }

    /// <summary>
    /// Reads a point of the sequence.
    /// </summary>
    public override IEnumerable<Element> VisitPoint(Unipi.MppgParser.Grammar.MppgParser.PointContext context)
    {
        var pointVisitor = new PointVisitor(_state);
        var point = context.Accept(pointVisitor);

        yield return point;
    }

    /// <summary>
    /// Reads a segment, whichever way its ends are bracketed.
    /// </summary>
    public override IEnumerable<Element> VisitSegment(Unipi.MppgParser.Grammar.MppgParser.SegmentContext context)
    {
        if (context.ChildCount != 1)
            throw new Exception("Expected 1 child expression");

        var segmentInnerContext = context.GetChild(0);
        var elements = segmentInnerContext.Accept(this);
        return elements;
    }

    /// <summary>
    /// Reads a segment including both its endpoints, written '[' … ']'.
    /// </summary>
    public override IEnumerable<Element> VisitSegmentLeftClosedRightClosed(
        Unipi.MppgParser.Grammar.MppgParser.SegmentLeftClosedRightClosedContext context)
    {
        var (leftPoint, rightPoint, slope, segmentText) = ParseSegment(context);
        var effectiveRightPoint = NormalizeRightPointIfNeeded(leftPoint, rightPoint, slope);

        yield return leftPoint;
        if (leftPoint.Time < effectiveRightPoint.Time)
        {
            yield return new Segment(leftPoint.Time, effectiveRightPoint.Time, leftPoint.Value, slope);
            if (!effectiveRightPoint.Time.IsPlusInfinite)
                yield return effectiveRightPoint;
        }
    }

    /// <summary>
    /// Reads a segment including its left endpoint alone, written '[' … '['.
    /// </summary>
    public override IEnumerable<Element> VisitSegmentLeftClosedRightOpen(
        Unipi.MppgParser.Grammar.MppgParser.SegmentLeftClosedRightOpenContext context)
    {
        var (leftPoint, rightPoint, slope, segmentText) = ParseSegment(context);
        var effectiveRightPoint = NormalizeRightPointIfNeeded(leftPoint, rightPoint, slope);

        yield return leftPoint;
        if (leftPoint.Time < effectiveRightPoint.Time)
            yield return new Segment(leftPoint.Time, effectiveRightPoint.Time, leftPoint.Value, slope);
    }

    /// <summary>
    /// Reads a segment including its right endpoint alone, written ']' … ']'.
    /// </summary>
    public override IEnumerable<Element> VisitSegmentLeftOpenRightClosed(
        Unipi.MppgParser.Grammar.MppgParser.SegmentLeftOpenRightClosedContext context)
    {
        var (leftPoint, rightPoint, slope, segmentText) = ParseSegment(context);
        var effectiveRightPoint = NormalizeRightPointIfNeeded(leftPoint, rightPoint, slope);

        if (leftPoint.Time < effectiveRightPoint.Time)
        {
            yield return new Segment(leftPoint.Time, effectiveRightPoint.Time, leftPoint.Value, slope);
            if (!effectiveRightPoint.Time.IsPlusInfinite)
                yield return effectiveRightPoint;
        }
    }

    /// <summary>
    /// Reads a segment including neither endpoint, written ']' … '['.
    /// </summary>
    public override IEnumerable<Element> VisitSegmentLeftOpenRightOpen(
        Unipi.MppgParser.Grammar.MppgParser.SegmentLeftOpenRightOpenContext context)
    {
        var (leftPoint, rightPoint, slope, segmentText) = ParseSegment(context);
        var effectiveRightPoint = NormalizeRightPointIfNeeded(leftPoint, rightPoint, slope);

        if (leftPoint.Time < effectiveRightPoint.Time)
            yield return new Segment(leftPoint.Time, effectiveRightPoint.Time, leftPoint.Value, slope);
    }

    private (Point leftPoint, Point rightPoint, Rational slope, string segmentText) ParseSegment(ParserRuleContext context)
    {
        var segmentText = context.GetJoinedText();

        var leftPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(0);
        var slopeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var rightPointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(1);

        var pointVisitor = new PointVisitor(_state);
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if (leftPoint.Time.IsInfinite)
            throw new InvalidOperationException($"Left endpoint cannot be infinite: {segmentText}");

        var slopeFromContext = EvaluateOptionalNumberExpression(slopeContext);
        var slope = GetSlope(leftPoint, rightPoint, slopeFromContext, segmentText);

        return (leftPoint, rightPoint, slope, segmentText);
    }

    private Rational? EvaluateOptionalNumberExpression(
        Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext? context)
    {
        if (context is null)
            return null;

        var expressionVisitor = new ExpressionVisitor(_state);
        var expression = context.Accept(expressionVisitor);
        if (expression is not RationalExpression rationalExpression)
            throw new InvalidOperationException("Expected a numeric expression in segment slope.");

        return rationalExpression.Compute();
    }

    private Point NormalizeRightPointIfNeeded(Point leftPoint, Point rightPoint, Rational slope)
    {
        if (!_normalizeInfiniteRightEndpoint || !rightPoint.Time.IsPlusInfinite)
            return rightPoint;

        return new Point(leftPoint.Time + 1, leftPoint.Value + slope);
    }

    private Rational GetSlope(Point leftPoint, Point rightPoint, Rational? slopeFromContext, string segmentText)
    {
        if (leftPoint.Time == rightPoint.Time)
        {
            if (leftPoint.Value != rightPoint.Value)
                throw new InvalidOperationException($"Invalid segment with zero length and different values: {segmentText}");
            return slopeFromContext ?? 0;
        }

        if (rightPoint.Time.IsPlusInfinite)
        {
            if (slopeFromContext is null)
            {
                if (leftPoint.Value != rightPoint.Value)
                    throw new InvalidOperationException($"Cannot infer slope for segment with infinite right endpoint and different values at endpoints: {segmentText}");
                return 0;
            }

            var s = slopeFromContext.Value;
            if (s < 0 && rightPoint.Value != Rational.MinusInfinity)
                throw new InvalidOperationException($"Specified slope should lead to a minus infinite value at infinite time: {segmentText}");
            if (s > 0 && rightPoint.Value != Rational.PlusInfinity)
                throw new InvalidOperationException($"Specified slope should lead to a plus infinite value at infinite time: {segmentText}");
            if (s == 0 && rightPoint.Value != leftPoint.Value)
                throw new InvalidOperationException($"Specified slope should lead to a constant value at infinite time: {segmentText}");

            return s;
        }

        Rational computedSlope;
        if (leftPoint.Value.IsInfinite || rightPoint.Value.IsInfinite)
        {
            if (leftPoint.Value == rightPoint.Value)
                computedSlope = 0;
            else
                throw new InvalidOperationException($"Invalid segment between {leftPoint} and {rightPoint}: {segmentText}");
        }
        else
            computedSlope = (rightPoint.Value - leftPoint.Value) / (rightPoint.Time - leftPoint.Time);

        if (slopeFromContext is not null && slopeFromContext.Value != computedSlope)
            throw new InvalidOperationException($"Specified slope does not match the slope computed from the endpoints: {segmentText}");

        return slopeFromContext ?? computedSlope;
    }
}
