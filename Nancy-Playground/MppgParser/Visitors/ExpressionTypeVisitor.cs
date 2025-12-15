using System.Text.RegularExpressions;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.MppgParser.Visitors;
class ExpressionTypeVisitor : MppgBaseVisitor<ExpressionType>
{
    public Dictionary<string, ExpressionType> State = new();
    
    public override ExpressionType VisitNumberVariableExp(Grammar.MppgParser.NumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }

    public override ExpressionType VisitEncNumberVariableExp(Grammar.MppgParser.EncNumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }

    public override ExpressionType VisitFunctionVariableExp(Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }
    
    public override ExpressionType VisitFunctionName(Grammar.MppgParser.FunctionNameContext context)
    {
        var name = context.GetChild(0).GetText();
        return State[name];
    }

    public override ExpressionType VisitNumberLiteral(Grammar.MppgParser.NumberLiteralContext context)
    {
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionBrackets(Grammar.MppgParser.FunctionBracketsContext context)
    {
        var innerType = context.GetChild(1).Accept(this);
        return innerType;
    }

    public override ExpressionType VisitNumberBrackets(Grammar.MppgParser.NumberBracketsContext context)
    {
        var innerType = context.GetChild(1).Accept(this);
        return innerType;
    }

    #region Function binary operators

    public override ExpressionType VisitFunctionMinimum(Grammar.MppgParser.FunctionMinimumContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMaximum(Grammar.MppgParser.FunctionMaximumContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMinPlusConvolution(Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMaxPlusConvolution(Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionMinPlusDeconvolution(Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a deconvolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionMaxPlusDeconvolution(Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionComposition(Grammar.MppgParser.FunctionCompositionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionScalarMultiplicationLeft(Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }
    
    public override ExpressionType VisitFunctionScalarMultiplicationRight(Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionScalarDivision(Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a deconvolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionSum(Grammar.MppgParser.FunctionSumContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionSubtraction(Grammar.MppgParser.FunctionSubtractionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    #endregion

    #region Function unary operators

    public override ExpressionType VisitFunctionSubadditiveClosure(Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionHShift(Grammar.MppgParser.FunctionHShiftContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionVShift(Grammar.MppgParser.FunctionVShiftContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionLowerPseudoInverse(Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionUpperPseudoInverse(Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionUpNonDecreasingClosure(Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionNonNegativeUpNonDecreasingClosure(Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionLeftExt(Grammar.MppgParser.FunctionLeftExtContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitFunctionRightExt(Grammar.MppgParser.FunctionRightExtContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    #endregion Function unary operators
    
    #region Function constructors

    public override ExpressionType VisitRateLatency(Grammar.MppgParser.RateLatencyContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitTokenBucket(Grammar.MppgParser.TokenBucketContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitAffineFunction(
        Grammar.MppgParser.AffineFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }
    
    public override ExpressionType VisitStepFunction(
        Grammar.MppgParser.StepFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitStairFunction(Grammar.MppgParser.StairFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }
    
    public override ExpressionType VisitDelayFunction(
        Grammar.MppgParser.DelayFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitZeroFunction(
        Grammar.MppgParser.ZeroFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    public override ExpressionType VisitEpsilonFunction(
        Grammar.MppgParser.EpsilonFunctionContext context)
    {
        // not ambiguous
        return ExpressionType.Function;
    }

    #endregion Function constructors

    #region Number-returning function operators

    public override ExpressionType VisitFunctionValueAt(Grammar.MppgParser.FunctionValueAtContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionLeftLimitAt(Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionRightLimitAt(Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionHorizontalDeviation(Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    public override ExpressionType VisitFunctionVerticalDeviation(Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        // not ambiguous
        return ExpressionType.Number;
    }

    #endregion
    
    #region Number binary operators

    public override ExpressionType VisitNumberMultiplication(Grammar.MppgParser.NumberMultiplicationContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a convolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitNumberDivision(Grammar.MppgParser.NumberDivisionContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            // either a deconvolution, or a function scaling
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitNumberSum(Grammar.MppgParser.NumberSumContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitNumberSub(Grammar.MppgParser.NumberSubContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitNumberMinimum(Grammar.MppgParser.NumberMinimumContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    public override ExpressionType VisitNumberMaximum(Grammar.MppgParser.NumberMaximumContext context)
    {
        var firstType = context.GetChild(0).Accept(this);
        var secondType = context.GetChild(2).Accept(this);

        if (firstType == ExpressionType.Function || secondType == ExpressionType.Function)
            return ExpressionType.Function;
        else
            return ExpressionType.Number;
    }

    #endregion
}