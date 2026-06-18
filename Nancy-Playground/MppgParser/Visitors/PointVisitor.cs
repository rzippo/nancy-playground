using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// In MPPG syntax, points are not used alone.
/// They are used to describe the endpoints of a segment, as well.
/// </summary>
public class PointVisitor : MppgBaseVisitor<Point>
{
    private readonly State _state;

    public PointVisitor(State? state = null)
    {
        _state = state ?? new State();
    }

    public override Point VisitEndpoint(Unipi.MppgParser.Grammar.MppgParser.EndpointContext context)
    {
        if (context.ChildCount != 5)
            throw new Exception("Expected 5 child expression");

        var timeContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var valueContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(1);

        var time = EvaluateNumberExpression(timeContext);
        var value = EvaluateNumberExpression(valueContext);

        return new Point(time, value);
    }

    public override Point VisitPoint(Unipi.MppgParser.Grammar.MppgParser.PointContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var endpointContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>(0);
        var point = endpointContext.Accept(this);

        return point;
    }

    private Rational EvaluateNumberExpression(
        Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext context)
    {
        var expressionVisitor = new ExpressionVisitor(_state);
        var expression = context.Accept(expressionVisitor);
        if (expression is not RationalExpression rationalExpression)
            throw new InvalidOperationException("Expected a numeric expression in point endpoint.");

        return rationalExpression.Compute();
    }
}
