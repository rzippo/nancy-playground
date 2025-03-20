using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionBrackets(Grammar.MppgParser.FunctionBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionMinPlusConvolution(
        Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");
        var isNotationAmbiguous =  context.GetChild(1).GetText() == "*";

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.Convolution(lCE, rCE);
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null && isNotationAmbiguous)
        {
            // this was mis-parsed: rational product
            var rationalExp = RationalExpression.Product(lRE, rRE);
            return (null, rationalExp);
        }
        else if (lCE is not null && rRE is not null && isNotationAmbiguous)
        {
            // this was mis-parsed: function scalar multiplication
            var curveExp = lCE.Scale(rRE);
            return (curveExp, null);
        }
        else if (lRE is not null && rCE is not null && isNotationAmbiguous)
        {
            // this was mis-parsed: function scalar multiplication
            var curveExp = rCE.Scale(lRE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionMaxPlusConvolution(
        Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.MaxPlusConvolution(lCE, rCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionMinPlusDeconvolution(
        Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");
        var isNotationAmbiguous =  context.GetChild(1).GetText() == "/";

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.Deconvolution(lCE, rCE);
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null && isNotationAmbiguous)
        {
            // this was mis-parsed: rational division
            var rationalExp = RationalExpression.Division(lRE, rRE);
            return (null, rationalExp);
        }
        else if (lCE is not null && rRE is not null && isNotationAmbiguous)
        {
            // this was mis-parsed: function scalar division
            var curveExp = lCE.Scale(rRE.Invert());
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionMaxPlusDeconvolution(
        Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.MaxPlusDeconvolution(lCE, rCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionComposition(
        Grammar.MppgParser.FunctionCompositionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.Composition(lCE, rCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionScalarMultiplicationLeft(
        Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rRE is not null)
        {
            var curveExp = lCE.Scale(rRE);
            return (curveExp, null);
        }
        if (lCE is not null && rCE is not null)
        {
            // this was mis-parsed: function min-plus convolution
            var curveExp = Expressions.Convolution(lCE, rCE);
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null)
        {
            // this was mis-parsed: rational product
            var rationalExp = RationalExpression.Product(lRE, rRE);
            return (null, rationalExp);
        }
        else if (lRE is not null && rCE is not null)
        {
            // this was mis-parsed: function scalar multiplication
            var curveExp = rCE.Scale(lRE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionScalarMultiplicationRight(
        Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lRE is not null && rCE is not null)
        {
            var curveExp = lCE.Scale(rRE);
            return (curveExp, null);
        }
        if (lCE is not null && rCE is not null)
        {
            // this was mis-parsed: function min-plus convolution
            var curveExp = Expressions.Convolution(lCE, rCE);
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null)
        {
            // this was mis-parsed: rational product
            var rationalExp = RationalExpression.Product(lRE, rRE);
            return (null, rationalExp);
        }
        else if (lCE is not null && rRE is not null)
        {
            // this was mis-parsed: function scalar multiplication
            var curveExp = rCE.Scale(lRE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionScalarDivision(
        Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);
        
        if (lCE is not null && rRE is not null)
        {
            var curveExp = lCE.Scale(rRE.Invert());
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null)
        {
            // this was mis-parsed: rational division
            var rationalExp = RationalExpression.Division(lRE, rRE);
            return (null, rationalExp);
        }
        else if (lCE is not null && rCE is not null)
        {
            // this was mis-parsed: function min-plus deconvolution
            var curveExp = Expressions.Deconvolution(lCE, rCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionSum(Grammar.MppgParser.FunctionSumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);

        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.Addition(lCE, rCE);
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null)
        {
            // this was mis-parsed
            var rationalExp = RationalExpression.Addition(lRE, rRE);
            return (null, rationalExp);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
    
    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionSubtraction(Grammar.MppgParser.FunctionSubtractionContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var (lCE, lRE) = context.GetChild(0).Accept(this);
        var (rCE, rRE) = context.GetChild(2).Accept(this);

        if (lCE is not null && rCE is not null)
        {
            var curveExp = Expressions.Subtraction(lCE, rCE, nonNegative: false);
            return (curveExp, null);
        }
        else if (lRE is not null && rRE is not null)
        {
            // this was mis-parsed
            var rationalExp = RationalExpression.Subtraction(lRE, rRE);
            return (null, rationalExp);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionSubadditiveClosure(
        Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var (lCE, _) = context.GetChild(2).Accept(this);
        if (lCE is not null)
        {
            var curveExp = Expressions.SubAdditiveClosure(lCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionLeftExt(Grammar.MppgParser.FunctionLeftExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var (lCE, _) = context.GetChild(2).Accept(this);
        if (lCE is not null)
        {
            var curveExp = Expressions.ToLeftContinuous(lCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }

    public override (CurveExpression? Function, RationalExpression? Number) VisitFunctionRightExt(Grammar.MppgParser.FunctionRightExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var (lCE, _) = context.GetChild(2).Accept(this);
        if (lCE is not null)
        {
            var curveExp = Expressions.ToRightContinuous(lCE);
            return (curveExp, null);
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetText()}\"");
        }
    }
}