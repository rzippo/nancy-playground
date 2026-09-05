using Antlr4.Runtime.Tree;
using Unipi.MppgParser.Grammar;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration.NancyCodeTreeBuilder;

namespace Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

internal sealed class ToNancyCodeTreeVisitor : MppgBaseVisitor<GeneratedCode>
{
    private readonly HashSet<string> _declaredVariables = [];
    private readonly SyntaxVersion _syntaxVersion;

    public ToNancyCodeTreeVisitor(SyntaxVersion syntaxVersion)
    {
        _syntaxVersion = syntaxVersion == default ? SyntaxVersion.Latest : syntaxVersion;
    }

    /// <summary>
    /// Any grammar rule without a dedicated Visit override reaches here instead of silently
    /// aggregating away its children's results. A rule with a single child is a pass-through
    /// wrapper (e.g. an unlabeled alternative like <c>functionExpression: functionSumExpression;</c>)
    /// and is forwarded as-is; anything else is a genuinely unported construct, and fails loudly
    /// so it is reported as NOT IMPLEMENTED at the statement level, rather than as a null downstream.
    /// </summary>
    public override GeneratedCode VisitChildren(IRuleNode node) =>
        node.ChildCount == 1
            ? node.GetChild(0).Accept(this)
            : throw new NotImplementedCodeGenerationException(node.GetText());

    protected override GeneratedCode DefaultResult =>
        throw new NotImplementedCodeGenerationException("<no code>");

    public CompilationUnitSyntax ToCompilationUnit(
        Unipi.MppgParser.Grammar.MppgParser.ProgramContext context)
    {
        var statementLines = context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.StatementLineContext>();
        var usesPlots = statementLines.UsesPlots();
        var usesImagePlots = statementLines.UsesImagePlots();
        var usesTikzPlots = statementLines.UsesTikzPlots();
        return NancyCodeTreeBuilder.ToCompilationUnit(
            context,
            this,
            GetPackageDirectives(usesImagePlots, usesTikzPlots),
            GetUsingNames(usesPlots, usesImagePlots, usesTikzPlots));
    }

    public override GeneratedCode VisitExpression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context) =>
        context.GetChild(0).Accept(this);

    public override GeneratedCode VisitFunctionEnclosedExpressionExp(
        Unipi.MppgParser.Grammar.MppgParser.FunctionEnclosedExpressionExpContext context) =>
        context.GetChild(0).Accept(this);

    public override GeneratedCode VisitStatementLine(
        Unipi.MppgParser.Grammar.MppgParser.StatementLineContext context) =>
        NancyCodeTreeBuilder.VisitStatementLine(context, this);

    public override GeneratedCode VisitAssignment(Unipi.MppgParser.Grammar.MppgParser.AssignmentContext context) =>
        NancyCodeTreeBuilder.VisitAssignment(context, this, _declaredVariables, GetDeclarationType);

    public override GeneratedCode VisitExpressionCommand(
        Unipi.MppgParser.Grammar.MppgParser.ExpressionCommandContext context) =>
        NancyCodeTreeBuilder.VisitExpressionCommand(context, this, PrintedExpression);

    public override GeneratedCode VisitPrintExpressionCommand(
        Unipi.MppgParser.Grammar.MppgParser.PrintExpressionCommandContext context) =>
        NancyCodeTreeBuilder.VisitPrintExpressionCommand(context);

    public override GeneratedCode VisitComment(Unipi.MppgParser.Grammar.MppgParser.CommentContext context) =>
        NancyCodeTreeBuilder.VisitComment(context);

    private static string GetDeclarationType(
        Unipi.MppgParser.Grammar.MppgParser.ExpressionContext expressionContext) =>
        expressionContext.GetExpressionType() switch
        {
            ExpressionType.Function => "Curve",
            ExpressionType.Number => "Rational",
            _ => "var"
        };

    private static ExpressionSyntax PrintedExpression(ExpressionSyntax expression, ExpressionType expressionType) =>
        expressionType == ExpressionType.Function
            ? CallMember(expression, "ToCodeString")
            : expression;

    public override GeneratedCode VisitEncNumberVariableExp(
        Unipi.MppgParser.Grammar.MppgParser.EncNumberVariableExpContext context) =>
        GeneratedCode.Expression(IdentifierName(context.GetChild(0).GetText()));

    public override GeneratedCode VisitFunctionVariableExp(
        Unipi.MppgParser.Grammar.MppgParser.FunctionVariableExpContext context) =>
        GeneratedCode.Expression(IdentifierName(context.GetChild(0).GetText()));

    public override GeneratedCode VisitNumberLiteral(
        Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        var visitor = new NumberLiteralVisitor();
        var number = context.Accept(visitor);
        var bareCode = number.ToBareIntCodeStringOrNull();
        return bareCode is not null
            ? GeneratedCode.Expression(ParseExpression(bareCode), isBareIntLiteral: true)
            : GeneratedCode.Expression(ParseExpression(number.ToExplicitCodeString()));
    }

    public override GeneratedCode VisitEncNumberBrackets(
        Unipi.MppgParser.Grammar.MppgParser.EncNumberBracketsContext context)
    {
        var inner = context.numberExpression().Accept(this);
        return GeneratedCode.Expression(ParenthesizedExpression(inner.SingleExpression()), inner.IsBareIntLiteral);
    }

    public override GeneratedCode VisitNumberPositive(
        Unipi.MppgParser.Grammar.MppgParser.NumberPositiveContext context) =>
        context.numberUnaryExpression().Accept(this);

    public override GeneratedCode VisitNumberNegative(
        Unipi.MppgParser.Grammar.MppgParser.NumberNegativeContext context)
    {
        var operand = context.numberUnaryExpression().Accept(this);
        var negated = PrefixUnaryExpression(
            SyntaxKind.UnaryMinusExpression,
            ParenthesizedExpression(operand.SingleExpression()));
        return GeneratedCode.Expression(negated, operand.IsBareIntLiteral);
    }

    public override GeneratedCode VisitNumberSumAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberSumAtomContext context) =>
        context.numberProductExpression().Accept(this);

    public override GeneratedCode VisitNumberProductAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberProductAtomContext context) =>
        context.numberUnaryExpression().Accept(this);

    public override GeneratedCode VisitNumberUnaryAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberUnaryAtomContext context) =>
        context.numberEnclosedExpression().Accept(this);

    public override GeneratedCode VisitNumberProductMulDiv(
        Unipi.MppgParser.Grammar.MppgParser.NumberProductMulDivContext context)
    {
        var leftCode = context.numberProductExpression().Accept(this);
        var rightCode = context.numberUnaryExpression().Accept(this);
        var left = leftCode.SingleExpression();
        var right = rightCode.SingleExpression();
        var bothBare = leftCode.IsBareIntLiteral && rightCode.IsBareIntLiteral;

        switch (context.op.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PROD_SIGN:
                return GeneratedCode.Expression(BinaryExpression(SyntaxKind.MultiplyExpression, left, right), bothBare);
            case Unipi.MppgParser.Grammar.MppgParser.DIV_SIGN:
            case Unipi.MppgParser.Grammar.MppgParser.DIV_OP:
                // Two bare ints would resolve to C#'s int division, truncating instead of computing
                // the exact Rational value: cast the left operand to Rational whenever neither side
                // is already Rational-typed on its own.
                if (bothBare)
                    left = CastToRational(left);
                return GeneratedCode.Expression(BinaryExpression(SyntaxKind.DivideExpression, left, right));
            case Unipi.MppgParser.Grammar.MppgParser.MOD_OP:
                return GeneratedCode.Expression(Invoke(Member(IdentifierName("Rational"), "Remainder"), left, right));
            default:
                throw new InvalidOperationException($"Unexpected operation: {context.op.Text}");
        }
    }

    private static ExpressionSyntax CastToRational(ExpressionSyntax value) =>
        CastExpression(IdentifierName("Rational"), ParenthesizedExpression(value));

    public override GeneratedCode VisitNumberSumSubMinMax(
        Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        var leftCode = context.GetChild(0).Accept(this);
        var rightCode = context.GetChild(2).Accept(this);
        var left = leftCode.SingleExpression();
        var right = rightCode.SingleExpression();
        var bothBare = leftCode.IsBareIntLiteral && rightCode.IsBareIntLiteral;

        return context.op.Type switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS =>
                GeneratedCode.Expression(BinaryExpression(SyntaxKind.AddExpression, left, right), bothBare),
            Unipi.MppgParser.Grammar.MppgParser.MINUS =>
                GeneratedCode.Expression(BinaryExpression(SyntaxKind.SubtractExpression, left, right), bothBare),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE =>
                GeneratedCode.Expression(Invoke(Member(IdentifierName("Rational"), "Min"), left, right)),
            Unipi.MppgParser.Grammar.MppgParser.VEE =>
                GeneratedCode.Expression(Invoke(Member(IdentifierName("Rational"), "Max"), left, right)),
            _ => throw new InvalidOperationException($"Unexpected operation: {context.op.Text}")
        };
    }

    public override GeneratedCode VisitEncNumberFloor(Unipi.MppgParser.Grammar.MppgParser.EncNumberFloorContext context) =>
        GeneratedCode.Expression(RationalCast(CallMember(
            context.numberExpression().Accept(this).SingleExpression(), "Floor")));

    public override GeneratedCode VisitEncNumberCeil(Unipi.MppgParser.Grammar.MppgParser.EncNumberCeilContext context) =>
        GeneratedCode.Expression(RationalCast(CallMember(
            context.numberExpression().Accept(this).SingleExpression(), "Ceil")));

    // Rational.Floor() and Rational.Ceil() return a BigInteger, which would make the operators around
    // them integer arithmetic: the cast back to Rational keeps the rest of the expression rational.
    private static ExpressionSyntax RationalCast(ExpressionSyntax value) =>
        ParenthesizedExpression(CastExpression(IdentifierName("Rational"), value));

    public override GeneratedCode VisitEncNumberAbs(Unipi.MppgParser.Grammar.MppgParser.EncNumberAbsContext context) =>
        GeneratedCode.Expression(Invoke(
            Member(IdentifierName("Rational"), "Abs"),
            context.numberExpression().Accept(this).SingleExpression()));

    public override GeneratedCode VisitEncNumberPow(Unipi.MppgParser.Grammar.MppgParser.EncNumberPowContext context)
    {
        var (baseExpr, exponent) = Operands(context.numberExpression());
        // Rational.Pow takes the exponent as a BigInteger, which the syntax has already required it to be.
        var bigIntegerExponent = CastExpression(
            ParseTypeName("System.Numerics.BigInteger"),
            ParenthesizedExpression(exponent));
        return GeneratedCode.Expression(Invoke(
            Member(IdentifierName("Rational"), "Pow"), baseExpr, bigIntegerExponent));
    }

    public override GeneratedCode VisitEncNumberGcd(Unipi.MppgParser.Grammar.MppgParser.EncNumberGcdContext context) =>
        GeneratedCode.Expression(BinaryRationalCode("GreatestCommonDivisor", Operands(context.numberExpression())));

    public override GeneratedCode VisitEncNumberLcm(Unipi.MppgParser.Grammar.MppgParser.EncNumberLcmContext context) =>
        GeneratedCode.Expression(BinaryRationalCode("LeastCommonMultiple", Operands(context.numberExpression())));

    private (ExpressionSyntax Left, ExpressionSyntax Right) Operands(
        Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext[] operands) =>
        (operands[0].Accept(this).SingleExpression(), operands[1].Accept(this).SingleExpression());

    private static ExpressionSyntax BinaryRationalCode(string method, (ExpressionSyntax Left, ExpressionSyntax Right) operands) =>
        Invoke(Member(IdentifierName("Rational"), method), operands.Left, operands.Right);

    public override GeneratedCode VisitRateLatency(Unipi.MppgParser.Grammar.MppgParser.RateLatencyContext context)
    {
        var rate = context.GetChild(2).Accept(this).SingleExpression();
        var latency = context.GetChild(4).Accept(this).SingleExpression();
        return GeneratedCode.Expression(ObjectCreate("RateLatencyServiceCurve", rate, latency));
    }

    public override GeneratedCode VisitTokenBucket(Unipi.MppgParser.Grammar.MppgParser.TokenBucketContext context)
    {
        var rate = context.GetChild(2).Accept(this).SingleExpression();
        var burst = context.GetChild(4).Accept(this).SingleExpression();
        return GeneratedCode.Expression(ObjectCreate("SigmaRhoArrivalCurve", burst, rate));
    }

    public override GeneratedCode VisitDelayFunction(Unipi.MppgParser.Grammar.MppgParser.DelayFunctionContext context)
    {
        var delay = context.GetChild(2).Accept(this).SingleExpression();
        return GeneratedCode.Expression(ObjectCreate("DelayServiceCurve", delay));
    }

    public override GeneratedCode VisitZeroFunction(Unipi.MppgParser.Grammar.MppgParser.ZeroFunctionContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "Zero")));

    public override GeneratedCode VisitEpsilonFunction(Unipi.MppgParser.Grammar.MppgParser.EpsilonFunctionContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "PlusInfinite")));

    public override GeneratedCode VisitAffineFunction(Unipi.MppgParser.Grammar.MppgParser.AffineFunctionContext context)
    {
        var slope = context.GetChild(2).Accept(this).SingleExpression();
        var constant = context.GetChild(4).Accept(this).SingleExpression();
        return GeneratedCode.Expression(ObjectCreate("Curve",
            ObjectCreate("Sequence", CollectionOf(
                ObjectCreate("Point", IntLiteral(0), constant),
                ObjectCreate("Segment", IntLiteral(0), IntLiteral(1), constant, slope))),
            IntLiteral(0), IntLiteral(1), slope));
    }

    public override GeneratedCode VisitStepFunction(Unipi.MppgParser.Grammar.MppgParser.StepFunctionContext context)
    {
        var o = context.GetChild(2).Accept(this).SingleExpression();
        var h = context.GetChild(4).Accept(this).SingleExpression();
        return GeneratedCode.Expression(ObjectCreate("StepCurve", h, o));
    }

    public override GeneratedCode VisitStairFunction(Unipi.MppgParser.Grammar.MppgParser.StairFunctionContext context)
    {
        var o = context.GetChild(2).Accept(this).SingleExpression();
        var l = context.GetChild(4).Accept(this).SingleExpression();
        var h = context.GetChild(6).Accept(this).SingleExpression();
        return GeneratedCode.Expression(CallMember(ObjectCreate("StairCurve", h, l), "DelayBy", o));
    }

    // Ultimately-affine/periodic function literals are themselves a full sub-grammar (breakpoints and
    // periods); rather than walk it again here, this reuses the existing parser that already turns
    // it into a Curve value, and splices its ToCodeString() rendering in as a parsed expression.
    public override GeneratedCode VisitUltimatelyAffineFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyAffineFunctionContext context) =>
        GeneratedCode.Expression(ParseExpression(ConcreteCurveCode(context.Accept(new ExpressionVisitor(null)))));

    public override GeneratedCode VisitUltimatelyPseudoPeriodicFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyPseudoPeriodicFunctionContext context) =>
        GeneratedCode.Expression(ParseExpression(ConcreteCurveCode(context.Accept(new ExpressionVisitor(null)))));

    private static string ConcreteCurveCode(IExpression expression) =>
        expression is ConcreteCurveExpression concreteCurve
            ? concreteCurve.Value.ToCodeString().UseNamedInfinityConstants()
            : throw new InvalidOperationException("Expected ConcreteCurveExpression");

    public override GeneratedCode VisitFunctionPositive(Unipi.MppgParser.Grammar.MppgParser.FunctionPositiveContext context) =>
        context.functionUnaryExpression().Accept(this);

    public override GeneratedCode VisitFunctionNegative(Unipi.MppgParser.Grammar.MppgParser.FunctionNegativeContext context) =>
        GeneratedCode.Expression(CallMember(context.functionUnaryExpression().Accept(this).SingleExpression(), "Negate"));

    public override GeneratedCode VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context) =>
        GeneratedCode.Expression(ParenthesizedExpression(context.GetChild(1).Accept(this).SingleExpression()));

    public override GeneratedCode VisitFunctionSubadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "SubAdditiveClosure"), context.GetChild(2).Accept(this).SingleExpression()));

    public override GeneratedCode VisitFunctionSuperadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSuperadditiveClosureContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "SuperAdditiveClosure"), context.GetChild(2).Accept(this).SingleExpression()));

    public override GeneratedCode VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context)
    {
        var curve = context.GetChild(2).Accept(this).SingleExpression();
        var shift = context.GetChild(4).Accept(this).SingleExpression();
        return GeneratedCode.Expression(CallMember(curve, "HorizontalShift", shift));
    }

    public override GeneratedCode VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context)
    {
        var curve = context.GetChild(2).Accept(this).SingleExpression();
        var shift = context.GetChild(4).Accept(this).SingleExpression();
        return GeneratedCode.Expression(CallMember(curve, "VerticalShift", shift));
    }

    public override GeneratedCode VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "LowerPseudoInverse"));

    public override GeneratedCode VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "UpperPseudoInverse"));

    public override GeneratedCode VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToUpperNonDecreasing"));

    public override GeneratedCode VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context) =>
        GeneratedCode.Expression(CallMember(
            CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToNonNegative"),
            "ToUpperNonDecreasing"));

    public override GeneratedCode VisitFunctionLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonDecreasingClosureContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToLowerNonDecreasing"));

    public override GeneratedCode VisitFunctionNonNegativeLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeLowNonDecreasingClosureContext context) =>
        GeneratedCode.Expression(CallMember(
            CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToNonNegative"),
            "ToLowerNonDecreasing"));

    public override GeneratedCode VisitFunctionUpNonIncreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonIncreasingClosureContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToUpperNonIncreasing"));

    public override GeneratedCode VisitFunctionLowNonIncreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonIncreasingClosureContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToLowerNonIncreasing"));

    public override GeneratedCode VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToLeftContinuous"));

    public override GeneratedCode VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "ToRightContinuous"));

    public override GeneratedCode VisitFunctionFloor(Unipi.MppgParser.Grammar.MppgParser.FunctionFloorContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "Floor"));

    public override GeneratedCode VisitFunctionCeil(Unipi.MppgParser.Grammar.MppgParser.FunctionCeilContext context) =>
        GeneratedCode.Expression(CallMember(context.GetChild(2).Accept(this).SingleExpression(), "Ceil"));

    public override GeneratedCode VisitFunctionName(Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext context) =>
        GeneratedCode.Expression(IdentifierName(context.GetChild(0).GetText()));

    public override GeneratedCode VisitFunctionValueAt(Unipi.MppgParser.Grammar.MppgParser.FunctionValueAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).SingleExpression();
        var time = context.GetChild(2).Accept(this).SingleExpression();
        return GeneratedCode.Expression(CallMember(curve, "ValueAt", time));
    }

    public override GeneratedCode VisitFunctionLeftLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).SingleExpression();
        var time = context.GetChild(2).Accept(this).SingleExpression();
        return GeneratedCode.Expression(CallMember(curve, "LeftLimitAt", time));
    }

    public override GeneratedCode VisitFunctionRightLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).SingleExpression();
        var time = context.GetChild(2).Accept(this).SingleExpression();
        return GeneratedCode.Expression(CallMember(curve, "RightLimitAt", time));
    }

    public override GeneratedCode VisitFunctionHorizontalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionHorizontalDeviationContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "HorizontalDeviation"),
            context.GetChild(2).Accept(this).SingleExpression(), context.GetChild(4).Accept(this).SingleExpression()));

    public override GeneratedCode VisitFunctionVerticalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionVerticalDeviationContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "VerticalDeviation"),
            context.GetChild(2).Accept(this).SingleExpression(), context.GetChild(4).Accept(this).SingleExpression()));

    public override GeneratedCode VisitFunctionZDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionZDeviationContext context) =>
        GeneratedCode.Expression(Invoke(Member(IdentifierName("Curve"), "ZDeviation"),
            context.GetChild(2).Accept(this).SingleExpression(), context.GetChild(4).Accept(this).SingleExpression()));

    public override GeneratedCode VisitFunctionSumChain(Unipi.MppgParser.Grammar.MppgParser.FunctionSumChainContext context)
    {
        var result = context.functionSumStart().Accept(this).SingleExpression();
        foreach (var suffix in context.functionSumSuffix())
        {
            result = suffix switch
            {
                Unipi.MppgParser.Grammar.MppgParser.FunctionSumSubMinMaxSuffixContext sum =>
                    ApplyFunctionFunctionSum(result, sum.op.Type, sum.functionProductExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxSuffixContext shift =>
                    ApplyFunctionNumberSum(result, shift.op.Type, shift.numberProductExpression().Accept(this).SingleExpression()),
                _ => throw new InvalidOperationException($"Unexpected function sum suffix: {suffix.GetType().Name}")
            };
        }
        return GeneratedCode.Expression(result);
    }

    public override GeneratedCode VisitFunctionShiftMinMaxRev(Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxRevContext context)
    {
        var first = context.numberExpression().Accept(this).SingleExpression();
        var second = context.functionProductExpression().Accept(this).SingleExpression();
        return GeneratedCode.Expression(ApplyNumberFunctionSum(first, context.op.Type, second));
    }

    public override GeneratedCode VisitFunctionProductChain(Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext context)
    {
        var result = context.functionProductStart().Accept(this).SingleExpression();
        foreach (var suffix in context.functionProductSuffix())
        {
            result = suffix switch
            {
                Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusConvolutionSuffixContext convolution =>
                    Invoke(Member(IdentifierName("Curve"), "Convolution"), result, convolution.functionUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulSuffixContext scalarMul =>
                    BinaryExpression(SyntaxKind.MultiplyExpression, result, scalarMul.numberUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusConvolutionSuffixContext maxConvolution =>
                    Invoke(Member(IdentifierName("Curve"), "MaxPlusConvolution"), result, maxConvolution.functionUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionMinPlusDeconvolutionSuffixContext deconvolution =>
                    Invoke(Member(IdentifierName("Curve"), "Deconvolution"), result, deconvolution.functionUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivSuffixContext scalarDiv =>
                    BinaryExpression(SyntaxKind.DivideExpression, result, scalarDiv.numberUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionMaxPlusDeconvolutionSuffixContext maxDeconvolution =>
                    Invoke(Member(IdentifierName("Curve"), "MaxPlusDeconvolution"), result, maxDeconvolution.functionUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionCompositionContext composition =>
                    Invoke(Member(IdentifierName("Curve"), "Composition"), result, composition.functionUnaryExpression().Accept(this).SingleExpression()),
                Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionSuffixContext scalarComposition =>
                    ConstantCurveCode(CallMember(result, "ValueAt", scalarComposition.numberUnaryExpression().Accept(this).SingleExpression())),
                _ => throw new InvalidOperationException($"Unexpected function product suffix: {suffix.GetType().Name}")
            };
        }
        return GeneratedCode.Expression(result);
    }

    public override GeneratedCode VisitFunctionScalarMulRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulRevContext context)
    {
        var first = context.numberProductExpression().Accept(this).SingleExpression();
        var second = context.functionUnaryExpression().Accept(this).SingleExpression();
        return GeneratedCode.Expression(BinaryExpression(SyntaxKind.MultiplyExpression, second, first));
    }

    public override GeneratedCode VisitFunctionScalarCompositionRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionRevContext context)
    {
        var first = context.numberProductExpression().Accept(this).SingleExpression();
        _ = context.functionUnaryExpression().Accept(this).SingleExpression();
        return GeneratedCode.Expression(ConstantCurveCode(first));
    }

    private static ExpressionSyntax ApplyFunctionFunctionSum(ExpressionSyntax left, int operationType, ExpressionSyntax right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS => BinaryExpression(SyntaxKind.AddExpression, left, right),
            Unipi.MppgParser.Grammar.MppgParser.MINUS => BinaryExpression(SyntaxKind.SubtractExpression, left, right),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE => Invoke(Member(IdentifierName("Curve"), "Minimum"), left, right),
            Unipi.MppgParser.Grammar.MppgParser.VEE => Invoke(Member(IdentifierName("Curve"), "Maximum"), left, right),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    private static ExpressionSyntax ApplyFunctionNumberSum(ExpressionSyntax left, int operationType, ExpressionSyntax right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS => CallMember(left, "VerticalShift", right),
            Unipi.MppgParser.Grammar.MppgParser.MINUS => CallMember(left, "VerticalShift",
                PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, ParenthesizedExpression(right))),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE => Invoke(Member(IdentifierName("Curve"), "Minimum"), left, ConstantCurveCode(right)),
            Unipi.MppgParser.Grammar.MppgParser.VEE => Invoke(Member(IdentifierName("Curve"), "Maximum"), left, ConstantCurveCode(right)),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    private static ExpressionSyntax ApplyNumberFunctionSum(ExpressionSyntax left, int operationType, ExpressionSyntax right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PLUS => CallMember(right, "VerticalShift", left),
            Unipi.MppgParser.Grammar.MppgParser.MINUS => CallMember(CallMember(right, "Negate"), "VerticalShift", left),
            Unipi.MppgParser.Grammar.MppgParser.WEDGE => Invoke(Member(IdentifierName("Curve"), "Minimum"), right, ConstantCurveCode(left)),
            Unipi.MppgParser.Grammar.MppgParser.VEE => Invoke(Member(IdentifierName("Curve"), "Maximum"), right, ConstantCurveCode(left)),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    public override GeneratedCode VisitAssertion(Unipi.MppgParser.Grammar.MppgParser.AssertionContext context) =>
        NancyCodeTreeBuilder.VisitAssertion(context, this, materialize: static value => value);

    public override GeneratedCode VisitStringExpression(Unipi.MppgParser.Grammar.MppgParser.StringExpressionContext context) =>
        NancyCodeTreeBuilder.VisitStringExpression(context);

    public override GeneratedCode VisitPlotCommand(Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context) =>
        NancyCodeTreeBuilder.VisitPlotCommand(context, this, materialize: static value => value, _declaredVariables);

    public override GeneratedCode VisitPlotTikzCommand(Unipi.MppgParser.Grammar.MppgParser.PlotTikzCommandContext context) =>
        NancyCodeTreeBuilder.VisitPlotTikzCommand(context, this, materialize: static value => value, _declaredVariables);

    private static IEnumerable<string> GetPackageDirectives(bool usesImagePlots, bool usesTikzPlots)
    {
        yield return $"#:package Unipi.Nancy@{PackageVersions.Nancy}";
        // Unipi.Nancy.Expressions already depends on Unipi.Nancy.Analyzers, so the --use-expressions
        // profile gets NANCY0005 (int/decimal division may lose precision) for free; this profile
        // does not, since Unipi.Nancy itself does not depend on it. Pinned explicitly here instead.
        // TODO: if Unipi.Nancy ever takes that dependency, drop this explicit pin.
        yield return $"#:package Unipi.Nancy.Analyzers@{PackageVersions.Analyzers}";
        if (usesImagePlots)
            yield return $"#:package Unipi.Nancy.Plots.ScottPlot@{PackageVersions.ScottPlot}";
        if (usesTikzPlots)
            yield return $"#:package Unipi.Nancy.Plots.Tikz@{PackageVersions.Tikz}";
    }

    private static IEnumerable<string> GetUsingNames(bool usesPlots, bool usesImagePlots, bool usesTikzPlots)
    {
        yield return "System.Globalization";
        if (usesPlots)
            yield return "System.IO";
        yield return "Unipi.Nancy.MinPlusAlgebra";
        yield return "Unipi.Nancy.NetworkCalculus";
        yield return "Unipi.Nancy.Numerics";
        if (usesImagePlots)
            yield return "Unipi.Nancy.Plots.ScottPlot";
        if (usesTikzPlots)
            yield return "Unipi.Nancy.Plots.Tikz";
    }
}
