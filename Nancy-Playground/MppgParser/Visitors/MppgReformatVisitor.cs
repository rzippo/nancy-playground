using Antlr4.Runtime.Tree;
using Unipi.MppgParser.Grammar;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Reformats an MPPG expression back to source text with explicit precedence and normalized spacing:
/// compound operands are parenthesized, call-shaped operators and rational literals are tight, and
/// binary operators are spaced.
/// </summary>
/// <remarks>
/// Operands are parenthesized only when they are themselves compound, i.e. they carry a binary
/// operator, so <c>x + y</c> stays bare while <c>x * y + z</c> becomes <c>(x * y) + z</c>.
/// A call-shaped operator renders as <c>bucket(2, 5)</c> and a fraction as <c>1/2</c>, while a
/// division of variables stays spaced, <c>x / y</c>.
/// Anything the visitor does not override — names, brackets, curve segments — is rendered by joining
/// its tokens with spaces.
/// </remarks>
public class MppgReformatVisitor : MppgBaseVisitor<string?>
{
    /// <summary>
    /// Reformats <paramref name="tree"/>, which must be an expression or a command subtree.
    /// </summary>
    public static string Reformat(IParseTree tree) => tree.Accept(new MppgReformatVisitor()) ?? string.Empty;

    public override string? VisitTerminal(ITerminalNode node) => node.GetText();

    protected override string? AggregateResult(string? aggregate, string? nextResult) =>
        string.IsNullOrEmpty(aggregate) ? nextResult : $"{aggregate} {nextResult}";

    private string Render(IParseTree tree) => tree.Accept(this) ?? string.Empty;

    public override string? VisitNumberSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        var left = context.numberExpression();
        var right = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberExpression(left))} {context.op.Text} {RenderOperand(right, IsCompoundNumberProduct(right))}";
    }

    public override string? VisitNumberProductMulDiv(Unipi.MppgParser.Grammar.MppgParser.NumberProductMulDivContext context)
    {
        var left = context.numberProductExpression();
        var right = context.numberUnaryExpression();

        // A division of two literals is a fraction, so it stays tight like a literal: 1/2, -3/2.
        if (context.op.Text == "/" && IsNumberLiteral(left) && IsNumberLiteral(right))
            return $"{Render(left)}/{Render(right)}";

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} {context.op.Text} {Render(right)}";
    }

    // A signed literal is one token value, so its sign is tight: -3, +2, -inf.
    public override string? VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context) =>
        context.ChildCount == 2
            ? $"{context.GetChild(0).GetText()}{context.GetChild(1).GetText()}"
            : context.GetChild(0).GetText();

    // Unary sign binds tightly to whatever it negates: -x, -(x + y), -f.
    public override string? VisitNumberPositive(Unipi.MppgParser.Grammar.MppgParser.NumberPositiveContext context) =>
        $"+{Render(context.numberUnaryExpression())}";

    public override string? VisitNumberNegative(Unipi.MppgParser.Grammar.MppgParser.NumberNegativeContext context) =>
        $"-{Render(context.numberUnaryExpression())}";

    public override string? VisitFunctionPositive(Unipi.MppgParser.Grammar.MppgParser.FunctionPositiveContext context) =>
        $"+{Render(context.functionUnaryExpression())}";

    public override string? VisitFunctionNegative(Unipi.MppgParser.Grammar.MppgParser.FunctionNegativeContext context) =>
        $"-{Render(context.functionUnaryExpression())}";

    // A rational literal is one value, so its division is tight: 1/2, -3/2.
    public override string? VisitRationalLiteral(Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext context) =>
        context.ChildCount == 3
            ? $"{Render(context.GetChild(0))}/{Render(context.GetChild(2))}"
            : Render(context.GetChild(0));

    // The commands are call-shaped too: printExpression(f), plot(f, out="p.png").
    public override string? VisitPrintExpressionCommand(Unipi.MppgParser.Grammar.MppgParser.PrintExpressionCommandContext context) =>
        RenderCall(context);

    public override string? VisitPlotCommand(Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context) =>
        RenderCall(context);

    public override string? VisitPlotTikzCommand(Unipi.MppgParser.Grammar.MppgParser.PlotTikzCommandContext context) =>
        RenderCall(context);

    // A plot option is a name bound to a value, so it is tight: out="p.png", xlim=[0, 10].
    public override string? VisitPlotOption(Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext context) =>
        $"{context.GetChild(0).GetText()}={Render(context.GetChild(2))}";

    public override string? VisitInterval(Unipi.MppgParser.Grammar.MppgParser.IntervalContext context) =>
        $"[{Render(context.rationalLiteral(0))}, {Render(context.rationalLiteral(1))}]";

    // Grouping brackets are tight like call parentheses: (x + y), not ( x + y ).
    public override string? VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context) =>
        $"({Render(context.functionExpression())})";

    public override string? VisitEncNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.EncNumberBracketsContext context) =>
        $"({Render(context.numberExpression())})";

    public override string? VisitFunctionSumChain(Unipi.MppgParser.Grammar.MppgParser.FunctionSumChainContext context)
    {
        var start = context.functionSumStart();
        var (result, compound) = start switch
        {
            Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxRevContext => (Render(start), true),
            Unipi.MppgParser.Grammar.MppgParser.FunctionSumFunctionStartContext s => (Render(start), IsCompoundFunctionProduct(s.functionProductExpression())),
            _ => throw new InvalidOperationException($"Unexpected function sum start: {start.GetType().Name}")
        };

        foreach (var suffix in context.functionSumSuffix())
        {
            var op = suffix.GetChild(0).GetText();
            var operand = suffix.GetChild(1);

            result = $"{MaybeWrap(result, compound)} {op} {RenderOperand(operand, IsCompoundProduct(operand))}";
            compound = true;
        }

        return result;
    }

    public override string? VisitFunctionShiftMinMaxRev(Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxRevContext context)
    {
        var left = context.numberProductExpression();
        var right = context.functionProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} {context.op.Text} {RenderOperand(right, IsCompoundFunctionProduct(right))}";
    }

    public override string? VisitFunctionProductChain(Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext context)
    {
        var start = context.functionProductStart();
        var (result, compound) = start switch
        {
            Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulRevContext or Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionRevContext => (Render(start), true),
            _ => (Render(start), false)
        };

        foreach (var suffix in context.functionProductSuffix())
        {
            var op = suffix.GetChild(0).GetText();
            var operand = suffix.GetChild(1);

            result = $"{MaybeWrap(result, compound)} {op} {Render(operand)}";
            compound = true;
        }

        return result;
    }

    public override string? VisitFunctionScalarMulRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulRevContext context)
    {
        var left = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} * {Render(context.functionUnaryExpression())}";
    }

    public override string? VisitFunctionScalarCompositionRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionRevContext context)
    {
        var left = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} comp {Render(context.functionUnaryExpression())}";
    }

    public override string? VisitRateLatency(Unipi.MppgParser.Grammar.MppgParser.RateLatencyContext context) => RenderCall(context);

    public override string? VisitTokenBucket(Unipi.MppgParser.Grammar.MppgParser.TokenBucketContext context) => RenderCall(context);

    public override string? VisitAffineFunction(Unipi.MppgParser.Grammar.MppgParser.AffineFunctionContext context) => RenderCall(context);

    public override string? VisitStepFunction(Unipi.MppgParser.Grammar.MppgParser.StepFunctionContext context) => RenderCall(context);

    public override string? VisitStairFunction(Unipi.MppgParser.Grammar.MppgParser.StairFunctionContext context) => RenderCall(context);

    public override string? VisitDelayFunction(Unipi.MppgParser.Grammar.MppgParser.DelayFunctionContext context) => RenderCall(context);

    public override string? VisitFunctionSubadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context) => RenderCall(context);

    public override string? VisitFunctionSuperadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSuperadditiveClosureContext context) => RenderCall(context);

    public override string? VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context) => RenderCall(context);

    public override string? VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context) => RenderCall(context);

    public override string? VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context) => RenderCall(context);

    public override string? VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context) => RenderCall(context);

    public override string? VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context) => RenderCall(context);

    public override string? VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context) => RenderCall(context);

    public override string? VisitFunctionLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonDecreasingClosureContext context) => RenderCall(context);

    public override string? VisitFunctionNonNegativeLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeLowNonDecreasingClosureContext context) => RenderCall(context);

    public override string? VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context) => RenderCall(context);

    public override string? VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context) => RenderCall(context);

    public override string? VisitFunctionFloor(Unipi.MppgParser.Grammar.MppgParser.FunctionFloorContext context) => RenderCall(context);

    public override string? VisitFunctionCeil(Unipi.MppgParser.Grammar.MppgParser.FunctionCeilContext context) => RenderCall(context);

    public override string? VisitFunctionValueAt(Unipi.MppgParser.Grammar.MppgParser.FunctionValueAtContext context) => RenderCall(context);

    public override string? VisitFunctionHorizontalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionHorizontalDeviationContext context) => RenderCall(context);

    public override string? VisitFunctionVerticalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionVerticalDeviationContext context) => RenderCall(context);

    public override string? VisitFunctionZDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionZDeviationContext context) => RenderCall(context);

    public override string? VisitEncNumberFloor(Unipi.MppgParser.Grammar.MppgParser.EncNumberFloorContext context) => RenderCall(context);

    public override string? VisitEncNumberCeil(Unipi.MppgParser.Grammar.MppgParser.EncNumberCeilContext context) => RenderCall(context);

    public override string? VisitEncNumberAbs(Unipi.MppgParser.Grammar.MppgParser.EncNumberAbsContext context) => RenderCall(context);

    public override string? VisitEncNumberPow(Unipi.MppgParser.Grammar.MppgParser.EncNumberPowContext context) => RenderCall(context);

    public override string? VisitEncNumberGcd(Unipi.MppgParser.Grammar.MppgParser.EncNumberGcdContext context) => RenderCall(context);

    public override string? VisitEncNumberLcm(Unipi.MppgParser.Grammar.MppgParser.EncNumberLcmContext context) => RenderCall(context);

    public override string? VisitFunctionLeftLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        var name = context.functionName().GetText();
        var arg = Render(context.numberExpression());
        var tilde = context.ChildCount == 6 ? "~" : "";

        return $"{name}({arg}{tilde}-)";
    }

    public override string? VisitFunctionRightLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        var name = context.functionName().GetText();
        var arg = Render(context.numberExpression());
        var tilde = context.ChildCount == 6 ? "~" : "";

        return $"{name}({arg}{tilde}+)";
    }

    public override string? VisitUltimatelyAffineFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyAffineFunctionContext context) =>
        $"uaf({Render(context.sequence())})";

    public override string? VisitUltimatelyPseudoPeriodicFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyPseudoPeriodicFunctionContext context)
    {
        var parts = new List<string>();

        var transient = context.uppTransientPart();
        if (transient is not null)
            parts.Add(Render(transient.sequence()));

        var periodic = context.uppPeriodicPart();
        parts.Add($"period({Render(periodic.sequence())})");

        var increment = context.increment();
        if (increment is not null)
        {
            parts.Add(Render(increment.rationalLiteral()));
            var length = increment.periodLenght();
            if (length is not null)
                parts.Add(Render(length.rationalLiteral()));
        }

        return $"upp({string.Join(", ", parts)})";
    }

    /// <summary>
    /// Renders a call-shaped rule as <c>name(arg, arg, ...)</c>, keeping the parentheses tight against
    /// the name and arguments while the arguments themselves keep their operator spacing.
    /// The name is the first child; every child that is not a parenthesis or comma is an argument.
    /// </summary>
    private string RenderCall(IParseTree context)
    {
        var name = context.GetChild(0).GetText();
        var args = new List<string>();

        for (var i = 1; i < context.ChildCount; i++)
        {
            var child = context.GetChild(i);
            if (child.GetText() is "(" or ")" or ",")
                continue;

            args.Add(Render(child));
        }

        return $"{name}({string.Join(", ", args)})";
    }

    private string RenderOperand(IParseTree tree, bool compound) =>
        compound ? $"({Render(tree)})" : Render(tree);

    private static string MaybeWrap(string text, bool compound) =>
        compound ? $"({text})" : text;

    private static bool IsCompoundNumberExpression(Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext context) =>
        context is Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext
        || (context is Unipi.MppgParser.Grammar.MppgParser.NumberSumAtomContext atom
            && IsCompoundNumberProduct(atom.numberProductExpression()));

    private static bool IsCompoundNumberProduct(Unipi.MppgParser.Grammar.MppgParser.NumberProductExpressionContext context) =>
        context is Unipi.MppgParser.Grammar.MppgParser.NumberProductMulDivContext;

    // True if the subtree is a bare number literal, i.e. a signed or unsigned literal with no
    // variable, call or operator in the way. Walked through the atom alternatives only, so a
    // division (numberProductMulDiv) or a sign applied to a non-literal is not one.
    private static bool IsNumberLiteral(IParseTree tree) => tree switch
    {
        Unipi.MppgParser.Grammar.MppgParser.NumberProductExpressionContext => tree.ChildCount == 1 && IsNumberLiteral(tree.GetChild(0)),
        Unipi.MppgParser.Grammar.MppgParser.NumberUnaryExpressionContext => tree.ChildCount == 1 && IsNumberLiteral(tree.GetChild(0)),
        Unipi.MppgParser.Grammar.MppgParser.EncNumberLiteralExpContext => true,
        _ => false
    };

    private static bool IsCompoundFunctionProduct(Unipi.MppgParser.Grammar.MppgParser.FunctionProductExpressionContext context) =>
        context is Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext chain
        && (chain.functionProductSuffix().Length > 0
            || chain.functionProductStart() is Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulRevContext or Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionRevContext);

    private static bool IsCompoundProduct(IParseTree operand) => operand switch
    {
        Unipi.MppgParser.Grammar.MppgParser.NumberProductExpressionContext number => IsCompoundNumberProduct(number),
        Unipi.MppgParser.Grammar.MppgParser.FunctionProductExpressionContext function => IsCompoundFunctionProduct(function),
        _ => false
    };
}
