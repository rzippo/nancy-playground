using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitFunctionHorizontalDeviation(
        Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ilE = context.GetChild(2).Accept(this);
        var irE = context.GetChild(4).Accept(this);
        
        if (ilE is CurveExpression lCE && irE is CurveExpression rCE)
        {
            var rationalExp = Expressions.HorizontalDeviation(lCE, rCE);
            return rationalExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }
    
    public override IExpression VisitFunctionVerticalDeviation(
        Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ilE = context.GetChild(2).Accept(this);
        var irE = context.GetChild(4).Accept(this);
        
        if (ilE is CurveExpression lCE && irE is CurveExpression rCE)
        {
            var rationalExp = Expressions.VerticalDeviation(lCE, rCE);
            return rationalExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }
}