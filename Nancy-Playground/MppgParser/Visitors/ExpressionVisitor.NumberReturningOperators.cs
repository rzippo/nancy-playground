using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionHorizontalDeviation(
        Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var (lCE, lRE) = context.GetChild(2).Accept(this);
        var (rCE, rRE) = context.GetChild(4).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var rationalExp = Expressions.HorizontalDeviation(lCE, rCE);
            return (null, rationalExp);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionVerticalDeviation(
        Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var (lCE, lRE) = context.GetChild(2).Accept(this);
        var (rCE, rRE) = context.GetChild(4).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var rationalExp = Expressions.VerticalDeviation(lCE, rCE);
            return (null, rationalExp);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
}