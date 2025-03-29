using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitNumberSum(Grammar.MppgParser.NumberSumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Addition(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Addition(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitNumberMinimum(Grammar.MppgParser.NumberMinimumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Minimum(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Min(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitNumberMaximum(Grammar.MppgParser.NumberMaximumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Maximum(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Max(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
}