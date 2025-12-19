using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitNumberMultiplication(Unipi.MppgParser.Grammar.MppgParser.NumberMultiplicationContext context)
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
                var curveExp = Expressions.Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Expressions.Scale(lCE, rRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
    public override IExpression VisitNumberDivision(Unipi.MppgParser.Grammar.MppgParser.NumberDivisionContext context)
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
                var curveExp = Expressions.Expressions.Deconvolution(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Expressions.Scale(lCE, rRE.Invert());
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
    public override IExpression VisitNumberSum(Unipi.MppgParser.Grammar.MppgParser.NumberSumContext context)
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
                var curveExp = Expressions.Expressions.Addition(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Expressions.VerticalShift(lCE, rRE);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Expressions.VerticalShift(rCE, lRE);
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
    
    public override IExpression VisitNumberSub(Unipi.MppgParser.Grammar.MppgParser.NumberSubContext context)
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
                var curveExp = Expressions.Expressions.Subtraction(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Expressions.VerticalShift(lCE, rRE.Negate());
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.Expressions.VerticalShift(rCE, lRE.Negate());
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
    
    public override IExpression VisitNumberMinimum(Unipi.MppgParser.Grammar.MppgParser.NumberMinimumContext context)
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
                var curveExp = Expressions.Expressions.Minimum(lCE, rCE);
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
    
    public override IExpression VisitNumberMaximum(Unipi.MppgParser.Grammar.MppgParser.NumberMaximumContext context)
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
                var curveExp = Expressions.Expressions.Maximum(lCE, rCE);
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