using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    /// <summary>
    /// Builds the expression a bracketed function holds.
    /// </summary>
    public override IExpression VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    /// <summary>
    /// Builds the expression a bracketed number holds, where it stands where a function may.
    /// </summary>
    public override IExpression VisitEncNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.EncNumberBracketsContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        return context.GetChild(1).Accept(this);
    }

    /// <summary>
    /// Builds a scalar multiplication written with the scalar first, as in <c>2 * f</c>.
    /// </summary>
    public override IExpression VisitFunctionScalarMulRev(
        Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulRevContext context)
    {
        var scalar = (RationalExpression)context.numberProductExpression().Accept(this);
        var curve = (CurveExpression)context.functionUnaryExpression().Accept(this);

        return curve.Scale(scalar);
    }

    /// <summary>
    /// Builds a composition with a scalar written first, which is the constant that scalar stands for.
    /// </summary>
    public override IExpression VisitFunctionScalarCompositionRev(
        Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionRevContext context)
    {
        var scalar = (RationalExpression)context.numberProductExpression().Accept(this);
        _ = context.functionUnaryExpression().Accept(this);

        return ConstantCurveExpression(scalar);
    }

    /// <summary>
    /// Builds a chain of sum-level operations, folding it left to right.
    /// </summary>
    public override IExpression VisitFunctionSumChain(
        Unipi.MppgParser.Grammar.MppgParser.FunctionSumChainContext context)
    {
        IExpression result = context.functionSumStart().Accept(this);

        foreach (var suffix in context.functionSumSuffix())
        {
            result = suffix switch
            {
                Unipi.MppgParser.Grammar.MppgParser.FunctionSumSubMinMaxSuffixContext sum =>
                    ApplyFunctionFunctionSum((CurveExpression)result, sum.op.Type,
                        (CurveExpression)sum.functionProductExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxSuffixContext shift =>
                    ApplyFunctionNumberSum((CurveExpression)result, shift.op.Type,
                        (RationalExpression)shift.numberProductExpression().Accept(this)),
                _ => throw new InvalidOperationException($"Unexpected function sum suffix: {suffix.GetType().Name}")
            };
        }

        return result;
    }

    /// <summary>
    /// Builds a sum-level operation with the scalar written first, as in <c>1 + f</c>.
    /// </summary>
    public override IExpression VisitFunctionShiftMinMaxRev(
        Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxRevContext context)
    {
        var scalar = (RationalExpression)context.numberProductExpression().Accept(this);
        var curve = (CurveExpression)context.functionProductExpression().Accept(this);

        return ApplyNumberFunctionSum(scalar, context.op.Type, curve);
    }

    /// <summary>
    /// Builds a chain of product-level operations, folding it left to right.
    /// </summary>
    public override IExpression VisitFunctionProductChain(
        Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext context)
    {
        IExpression result = context.functionProductStart().Accept(this);

        foreach (var suffix in context.functionProductSuffix())
        {
            result = suffix switch
            {
                Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusConvolutionSuffixContext convolution =>
                    Expressions.Expressions.Convolution((CurveExpression)result,
                        (CurveExpression)convolution.functionUnaryExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulSuffixContext scalarMul =>
                    ((CurveExpression)result).Scale(
                        (RationalExpression)scalarMul.numberUnaryExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusConvolutionSuffixContext convolution =>
                    Expressions.Expressions.MaxPlusConvolution((CurveExpression)result,
                        (CurveExpression)convolution.functionUnaryExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusDeconvolutionSuffixContext deconvolution =>
                    Expressions.Expressions.Deconvolution((CurveExpression)result,
                        (CurveExpression)deconvolution.functionUnaryExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivSuffixContext scalarDiv =>
                    ((CurveExpression)result).Scale(
                        ((RationalExpression)scalarDiv.numberUnaryExpression().Accept(this)).Invert()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusDeconvolutionSuffixContext deconvolution =>
                    Expressions.Expressions.MaxPlusDeconvolution((CurveExpression)result,
                        (CurveExpression)deconvolution.functionUnaryExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionCompositionContext composition =>
                    Expressions.Expressions.Composition((CurveExpression)result,
                        (CurveExpression)composition.functionUnaryExpression().Accept(this)),
                Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionSuffixContext scalarComposition =>
                    ConstantCurveExpression(
                        ((CurveExpression)result).ValueAt(
                            (RationalExpression)scalarComposition.numberUnaryExpression().Accept(this))),
                _ => throw new InvalidOperationException($"Unexpected function product suffix: {suffix.GetType().Name}")
            };
        }

        return result;
    }

    private static IExpression ApplyFunctionFunctionSum(
        CurveExpression left,
        int operationType,
        CurveExpression right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS => Expressions.Expressions.Addition(left, right),
            Unipi.MppgParser.Grammar.MppgParser.MINUS => Expressions.Expressions.Subtraction(left, right),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE => Expressions.Expressions.Minimum(left, right),
            Unipi.MppgParser.Grammar.MppgParser.VEE => Expressions.Expressions.Maximum(left, right),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    private static IExpression ApplyFunctionNumberSum(
        CurveExpression left,
        int operationType,
        RationalExpression right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS => Expressions.Expressions.VerticalShift(left, right),
            Unipi.MppgParser.Grammar.MppgParser.MINUS => Expressions.Expressions.VerticalShift(left, right.Negate()),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE => Expressions.Expressions.Minimum(left, new PureConstantCurve(right.Compute())),
            Unipi.MppgParser.Grammar.MppgParser.VEE => Expressions.Expressions.Maximum(left, new PureConstantCurve(right.Compute())),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    private static IExpression ApplyNumberFunctionSum(
        RationalExpression left,
        int operationType,
        CurveExpression right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS => Expressions.Expressions.VerticalShift(right, left),
            Unipi.MppgParser.Grammar.MppgParser.MINUS => Expressions.Expressions.VerticalShift(right.Negate(), left),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE => Expressions.Expressions.Minimum(right, new PureConstantCurve(left.Compute())),
            Unipi.MppgParser.Grammar.MppgParser.VEE => Expressions.Expressions.Maximum(right, new PureConstantCurve(left.Compute())),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    private static CurveExpression ConstantCurveExpression(RationalExpression value) =>
        Expressions.Expressions.FromCurve(new PureConstantCurve(value.Compute()));
	    
    /// <summary>
    /// Builds the expression of <c>star</c>, also spelled <c>subaddclosure</c>, the sub-additive closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionSubadditiveClosure(
        Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.SubAdditiveClosure(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>superaddclosure</c>, the super-additive closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionSuperadditiveClosure(
        Unipi.MppgParser.Grammar.MppgParser.FunctionSuperadditiveClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.SuperAdditiveClosure(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>hShift</c>, a horizontal shift.
    /// </summary>
    public override IExpression VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context)
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

    /// <summary>
    /// Builds the expression of <c>vShift</c>, a vertical shift.
    /// </summary>
    public override IExpression VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context)
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

    /// <summary>
    /// Builds the expression of <c>inv</c>, also spelled <c>low_inv</c>, the lower pseudo-inverse of a curve.
    /// </summary>
    public override IExpression VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.LowerPseudoInverse(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>up_inv</c>, the upper pseudo-inverse of a curve.
    /// </summary>
    public override IExpression VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.UpperPseudoInverse(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>upclosure</c>, the upward non-decreasing closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
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

    /// <summary>
    /// Builds the expression of <c>nnupclosure</c>, the non-negative upward non-decreasing closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
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

    /// <summary>
    /// Builds the expression of <c>lowclosure</c>, the downward non-decreasing closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonDecreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToLowerNonDecreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>nnlowclosure</c>, the non-negative downward non-decreasing closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionNonNegativeLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeLowNonDecreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE
                .ToNonNegative()
                .ToLowerNonDecreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>upnoninc</c>/<c>upnonincclosure</c>, the upward non-increasing closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionUpNonIncreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonIncreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToUpperNonIncreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>lownoninc</c>/<c>lownonincclosure</c>, the downward non-increasing closure of a curve.
    /// </summary>
    public override IExpression VisitFunctionLowNonIncreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonIncreasingClosureContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = lCE.ToLowerNonIncreasing();
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>left-ext</c>, the left extension of a curve.
    /// </summary>
    public override IExpression VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context)
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

    /// <summary>
    /// Builds the expression of <c>right-ext</c>, the right extension of a curve.
    /// </summary>
    public override IExpression VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context)
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

    /// <summary>
    /// Builds the expression of <c>floor</c> applied to a curve.
    /// </summary>
    public override IExpression VisitFunctionFloor(Unipi.MppgParser.Grammar.MppgParser.FunctionFloorContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.Floor(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the expression of <c>ceil</c> applied to a curve.
    /// </summary>
    public override IExpression VisitFunctionCeil(Unipi.MppgParser.Grammar.MppgParser.FunctionCeilContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var ilCE = context.GetChild(2).Accept(this);
        if (ilCE is CurveExpression lCE)
        {
            var curveExp = Expressions.Expressions.Ceil(lCE);
            return curveExp;
        }
        else
        {
            throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
        }
    }

    /// <summary>
    /// Builds the negation of a curve, as in <c>-f</c>.
    /// </summary>
    public override IExpression VisitFunctionNegative(Unipi.MppgParser.Grammar.MppgParser.FunctionNegativeContext context)
    {
        var ie = base.VisitFunctionNegative(context);
        return ie switch
        {
            // shortcut for negated literals
            RationalNumberExpression rne => new RationalNumberExpression(-rne.Value),
            RationalExpression re => re.Negate(), // todo: support - operator
            CurveExpression ce => ce.Negate(), // todo: support - operator
            _ => throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"")
        };
    }
}
