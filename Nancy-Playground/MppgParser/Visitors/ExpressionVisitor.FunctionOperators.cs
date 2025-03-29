using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitFunctionBrackets(Grammar.MppgParser.FunctionBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    public override IExpression VisitFunctionMinPlusConvolution(
        Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");
        var isNotationAmbiguous =  context.GetChild(1).GetText() == "*";

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: rational product
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = lCE.Scale(rRE);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE) when isNotationAmbiguous:
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = rCE.Scale(lRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionMaxPlusConvolution(
        Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.MaxPlusConvolution(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionMinPlusDeconvolution(
        Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");
        var isNotationAmbiguous =  context.GetChild(1).GetText() == "/";

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Deconvolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: rational division
                var rationalExp = RationalExpression.Division(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, RationalExpression rRE) when isNotationAmbiguous:
            {
                // this was mis-parsed: function scalar division
                var curveExp = lCE.Scale(rRE.Invert());
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionMaxPlusDeconvolution(
        Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.MaxPlusDeconvolution(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionComposition(
        Grammar.MppgParser.FunctionCompositionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Composition(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionScalarMultiplicationLeft(
        Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var curveExp = lCE.Scale(rRE);
                return curveExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed: function min-plus convolution
                var curveExp = Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed: rational product
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = rCE.Scale(lRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionScalarMultiplicationRight(
        Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var curveExp = lCE.Scale(rRE);
                return curveExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed: function min-plus convolution
                var curveExp = Expressions.Convolution(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed: rational product
                var rationalExp = RationalExpression.Product(lRE, rRE);
                return rationalExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed: function scalar multiplication
                var curveExp = rCE.Scale(lRE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionScalarDivision(
        Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var curveExp = lCE.Scale(rRE.Invert());
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed: rational division
                var rationalExp = RationalExpression.Division(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, CurveExpression rCE):
            {
                // this was mis-parsed: function min-plus deconvolution
                var curveExp = Expressions.Deconvolution(lCE, rCE);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionSum(Grammar.MppgParser.FunctionSumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Addition(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed
                var rationalExp = RationalExpression.Addition(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionSubtraction(Grammar.MppgParser.FunctionSubtractionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Subtraction(lCE, rCE, nonNegative: false);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed
                var rationalExp = RationalExpression.Subtraction(lRE, rRE);
                return rationalExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionSubadditiveClosure(
        Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.SubAdditiveClosure(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }

    public override IExpression VisitFunctionLeftExt(Grammar.MppgParser.FunctionLeftExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.ToLeftContinuous(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }

    public override IExpression VisitFunctionRightExt(Grammar.MppgParser.FunctionRightExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.ToRightContinuous(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
}