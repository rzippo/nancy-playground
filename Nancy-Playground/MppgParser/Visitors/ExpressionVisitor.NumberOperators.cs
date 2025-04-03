using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitNumberMultiplication(Grammar.MppgParser.NumberMultiplicationContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Scale(lCE, rRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
    public override IExpression VisitNumberDivision(Grammar.MppgParser.NumberDivisionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Division(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Deconvolution(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Scale(lCE, rRE.Invert());
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
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
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Shift(lCE, rRE);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Shift(rCE, lRE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Addition(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
    public override IExpression VisitNumberSub(Grammar.MppgParser.NumberSubContext context)
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
                var curveExp = Expressions.Subtraction(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Shift(lCE, rRE.Negate());
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Shift(rCE, lRE.Negate());
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                var rationalExp = RationalExpression.Subtraction(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
}