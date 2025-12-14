using System.Net;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.MppgParser.Visitors;

/// <summary>
/// In MPPG syntax, points are not used alone.
/// They are used to describe the endpoints of a segment, as well.
/// </summary>
public class PointVisitor : MppgBaseVisitor<Point>
{
    public override Point VisitEndpoint(Grammar.MppgParser.EndpointContext context)
    {
        if (context.ChildCount != 5)
            throw new Exception("Expected 5 child expression");

        var timeContext = context.GetChild(1);
        var valueContext = context.GetChild(3);
        
        var numberLiteralVisitor = new NumberLiteralVisitor();
        var time = numberLiteralVisitor.Visit(timeContext);
        var value = numberLiteralVisitor.Visit(valueContext);
        
        return new Point(time, value);
    }

    public override Point VisitPoint(Grammar.MppgParser.PointContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var endpointContext = context.GetChild<Grammar.MppgParser.EndpointContext>(0);
        var point = endpointContext.Accept(this);
        
        return point;
    }
}