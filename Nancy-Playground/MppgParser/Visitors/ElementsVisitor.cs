using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.MppgParser.Visitors;

/// <summary>
/// In MPPG, each end of a segment may be either inclusive or exclusive.
/// So, rather than just a <see cref="Segment"/>, they are a sequence of <see cref="Element"/>s, including <see cref="Point"/>s as well.
/// </summary>
public class ElementsVisitor : MppgBaseVisitor<IEnumerable<Element>>
{
    public override IEnumerable<Element> VisitSequence(Grammar.MppgParser.SequenceContext context)
    {
        var elements = Enumerable.Empty<Element>();
        for (int i = 0; i < context.ChildCount; i++)
        {
            var segmentContext = context.GetChild(i);
            elements = elements.Concat(Visit(segmentContext));
        }
        return elements;
    }

    public override IEnumerable<Element> VisitSegment(Grammar.MppgParser.SegmentContext context)
    {
        if(context.ChildCount != 1)
            throw new Exception("Expected 1 child expression");

        var segmentInnerContext = context.GetChild(0);
        var elements = segmentInnerContext.Accept(this);
        return elements;
    }

    public override IEnumerable<Element> VisitSegmentLeftClosedRightClosed(Grammar.MppgParser.SegmentLeftClosedRightClosedContext context)
    {
        if (context.ChildCount != 5)
            throw new Exception("Expected 5 child expression");

        var leftPointContext = context.GetChild(1);
        var slopeContext = context.GetChild(2);
        var rightPointContext = context.GetChild(3);
        
        var pointVisitor = new PointVisitor();
        var numberLiteralVisitor = new NumberLiteralVisitor();
        
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var slope = numberLiteralVisitor.Visit(slopeContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if (rightPoint.Time.IsInfinite)
            rightPoint = new Point(leftPoint.Time + 1, leftPoint.Value + slope); 

        var segment = SegmentFromEndPointsAndSlope(leftPoint, slope, rightPoint);

        yield return leftPoint;
        yield return segment;
        yield return rightPoint;
    }

    public override IEnumerable<Element> VisitSegmentLeftClosedRightOpen(Grammar.MppgParser.SegmentLeftClosedRightOpenContext context)
    {
        if (context.ChildCount != 5)
            throw new Exception("Expected 5 child expression");

        var leftPointContext = context.GetChild(1);
        var slopeContext = context.GetChild(2);
        var rightPointContext = context.GetChild(3);
        
        var pointVisitor = new PointVisitor();
        var numberLiteralVisitor = new NumberLiteralVisitor();
        
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var slope = numberLiteralVisitor.Visit(slopeContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if (rightPoint.Time.IsInfinite)
            rightPoint = new Point(leftPoint.Time + 1, leftPoint.Value + slope); 

        var segment = SegmentFromEndPointsAndSlope(leftPoint, slope, rightPoint);

        yield return leftPoint;
        yield return segment;
    }

    public override IEnumerable<Element> VisitSegmentLeftOpenRightClosed(Grammar.MppgParser.SegmentLeftOpenRightClosedContext context)
    {
        if (context.ChildCount != 5)
            throw new Exception("Expected 5 child expression");

        var leftPointContext = context.GetChild(1);
        var slopeContext = context.GetChild(2);
        var rightPointContext = context.GetChild(3);
        
        var pointVisitor = new PointVisitor();
        var numberLiteralVisitor = new NumberLiteralVisitor();
        
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var slope = numberLiteralVisitor.Visit(slopeContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if (rightPoint.Time.IsInfinite)
            rightPoint = new Point(leftPoint.Time + 1, leftPoint.Value + slope); 

        var segment = SegmentFromEndPointsAndSlope(leftPoint, slope, rightPoint);

        yield return segment;
        yield return rightPoint;
    }

    public override IEnumerable<Element> VisitSegmentLeftOpenRightOpen(Grammar.MppgParser.SegmentLeftOpenRightOpenContext context)
    {
        if (context.ChildCount != 5)
            throw new Exception("Expected 5 child expression");

        var leftPointContext = context.GetChild(1);
        var slopeContext = context.GetChild(2);
        var rightPointContext = context.GetChild(3);
        
        var pointVisitor = new PointVisitor();
        var numberLiteralVisitor = new NumberLiteralVisitor();
        
        var leftPoint = pointVisitor.Visit(leftPointContext);
        var slope = numberLiteralVisitor.Visit(slopeContext);
        var rightPoint = pointVisitor.Visit(rightPointContext);

        if (rightPoint.Time.IsInfinite)
            rightPoint = new Point(leftPoint.Time + 1, leftPoint.Value + slope); 

        var segment = SegmentFromEndPointsAndSlope(leftPoint, slope, rightPoint);

        yield return segment;
    }

    public static Segment SegmentFromEndPointsAndSlope(Point left, Rational slope, Point right)
    {
        if (left.Time.IsInfinite || right.Time.IsInfinite)
            throw new InvalidOperationException("Cannot build segment with infinite times.");

        var segment = new Segment(
            startTime: left.Time,
            endTime: right.Time,
            rightLimitAtStartTime: left.Value,
            slope: slope
        );
        return segment;
    }
}