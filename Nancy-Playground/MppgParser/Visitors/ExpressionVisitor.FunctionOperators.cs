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

    public override IExpression VisitNumberBrackets(Grammar.MppgParser.NumberBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }
    
    public override IExpression VisitEncNumberBrackets(Grammar.MppgParser.EncNumberBracketsContext context)
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }

    public override IExpression VisitFunctionMinimum(Grammar.MppgParser.FunctionMinimumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Minimum(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed
                var rationalExp = RationalExpression.Min(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var constantCurve = new PureConstantCurve(rRE.Compute());
                var curveExp = Expressions.Minimum(lCE, constantCurve);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                var constantCurve = new PureConstantCurve(lRE.Compute());
                var curveExp = Expressions.Minimum(rCE, constantCurve);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
        }
    }
    
    public override IExpression VisitFunctionMaximum(Grammar.MppgParser.FunctionMaximumContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);

        switch (ilE, irE)
        {
            case (CurveExpression lCE, CurveExpression rCE):
            {
                var curveExp = Expressions.Maximum(lCE, rCE);
                return curveExp;
            }
            case (RationalExpression lRE, RationalExpression rRE):
            {
                // this was mis-parsed
                var rationalExp = RationalExpression.Max(lRE, rRE);
                return rationalExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                var constantCurve = new PureConstantCurve(rRE.Compute());
                var curveExp = Expressions.Maximum(lCE, constantCurve);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                var constantCurve = new PureConstantCurve(lRE.Compute());
                var curveExp = Expressions.Maximum(rCE, constantCurve);
                return curveExp;
            }
            default:
            {
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.VerticalShift(lCE, rRE);
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.VerticalShift(rCE, lRE);
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
                var curveExp = Expressions.Subtraction(lCE, rCE);
                return curveExp;
            }
            case (CurveExpression lCE, RationalExpression rRE):
            {
                // this was mis-parsed
                var curveExp = Expressions.VerticalShift(lCE, rRE.Negate());
                return curveExp;
            }
            case (RationalExpression lRE, CurveExpression rCE):
            {
                // this was mis-parsed
                var curveExp = Expressions.VerticalShift(rCE, lRE.Negate());
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
                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
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
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionHShift(Grammar.MppgParser.FunctionHShiftContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ifExp = context.GetChild(2).Accept(this);
        var ishiftExp = context.GetChild(4).Accept(this);

        if (ifExp is not CurveExpression fExp || ishiftExp is not RationalExpression shiftExp)
            throw new Exception("Expected f and shift expressions");

        var curveExp = fExp.HorizontalShift(shiftExp);
        return curveExp;
    }

    public override IExpression VisitFunctionVShift(Grammar.MppgParser.FunctionVShiftContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var ifExp = context.GetChild(2).Accept(this);
        var ishiftExp = context.GetChild(4).Accept(this);

        if (ifExp is not CurveExpression fExp || ishiftExp is not RationalExpression shiftExp)
            throw new Exception("Expected f and shift expressions");

        var curveExp = fExp.VerticalShift(shiftExp);
        return curveExp;
    }

    public override IExpression VisitFunctionLowerPseudoInverse(Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.LowerPseudoInverse(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionUpperPseudoInverse(Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.UpperPseudoInverse(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionUpNonDecreasingClosure(Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToUpperNonDecreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }
    
    public override IExpression VisitFunctionNonNegativeUpNonDecreasingClosure(Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE
                .ToNonNegative()
                .ToUpperNonDecreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }
    
    public override IExpression VisitFunctionLeftExt(Grammar.MppgParser.FunctionLeftExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToLeftContinuous();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    public override IExpression VisitFunctionRightExt(Grammar.MppgParser.FunctionRightExtContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToRightContinuous();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }
}