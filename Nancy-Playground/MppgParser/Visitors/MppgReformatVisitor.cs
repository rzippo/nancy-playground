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

    /// <summary>
    /// Writes a token back as it was written.
    /// </summary>
    public override string? VisitTerminal(ITerminalNode node) => node.GetText();

    /// <summary>
    /// Joins what the children wrote, which is what a rule with no override of its own produces.
    /// </summary>
    protected override string? AggregateResult(string? aggregate, string? nextResult) =>
        string.IsNullOrEmpty(aggregate) ? nextResult : $"{aggregate} {nextResult}";

    private string Render(IParseTree tree) => tree.Accept(this) ?? string.Empty;

    /// <summary>
    /// Writes a sum, a subtraction, a minimum or a maximum between numbers.
    /// </summary>
    public override string? VisitNumberSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        var left = context.numberExpression();
        var right = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberExpression(left))} {context.op.Text} {RenderOperand(right, IsCompoundNumberProduct(right))}";
    }

    /// <summary>
    /// Writes a multiplication or a division between numbers.
    /// </summary>
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
    /// <summary>
    /// Writes a number literal.
    /// </summary>
    public override string? VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context) =>
        context.ChildCount == 2
            ? $"{context.GetChild(0).GetText()}{context.GetChild(1).GetText()}"
            : context.GetChild(0).GetText();

    // Unary sign binds tightly to whatever it negates: -x, -(x + y), -f.
    /// <summary>
    /// Writes a number with its sign, where the sign is a plus.
    /// </summary>
    public override string? VisitNumberPositive(Unipi.MppgParser.Grammar.MppgParser.NumberPositiveContext context) =>
        $"+{Render(context.numberUnaryExpression())}";

    /// <summary>
    /// Writes a number with its sign, where the sign is a minus.
    /// </summary>
    public override string? VisitNumberNegative(Unipi.MppgParser.Grammar.MppgParser.NumberNegativeContext context) =>
        $"-{Render(context.numberUnaryExpression())}";

    /// <summary>
    /// Writes a curve with its sign, where the sign is a plus.
    /// </summary>
    public override string? VisitFunctionPositive(Unipi.MppgParser.Grammar.MppgParser.FunctionPositiveContext context) =>
        $"+{Render(context.functionUnaryExpression())}";

    /// <summary>
    /// Writes a curve with its sign, where the sign is a minus.
    /// </summary>
    public override string? VisitFunctionNegative(Unipi.MppgParser.Grammar.MppgParser.FunctionNegativeContext context) =>
        $"-{Render(context.functionUnaryExpression())}";

    // An assertion is call-shaped around a comparison: assert(f * g = g * f).
    /// <summary>
    /// Writes an <c>assert</c> command, either the two-sided comparison or the one-sided
    /// <c>is</c>/<c>is not</c> property form.
    /// </summary>
    public override string? VisitAssertion(Unipi.MppgParser.Grammar.MppgParser.AssertionContext context)
    {
        var tail = context.assertionTail();

        if (tail.propertyName() is { } propertyName)
        {
            var not = tail.notKeyword() is not null ? "not " : "";
            return $"assert({Render(context.expression())} is {not}{propertyName.GetText()})";
        }

        return $"assert({Render(context.expression())} {tail.assertionOperator().GetText()} {Render(tail.expression())})";
    }

    // The commands are call-shaped too: printExpression(f), plot(f, out="p.png").
    /// <summary>
    /// Writes a <c>printExpression</c> command.
    /// </summary>
    public override string? VisitPrintExpressionCommand(Unipi.MppgParser.Grammar.MppgParser.PrintExpressionCommandContext context) =>
        RenderCall(context);

    /// <summary>
    /// Writes a <c>plot</c> command.
    /// </summary>
    public override string? VisitPlotCommand(Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context) =>
        RenderCall(context);

    /// <summary>
    /// Writes a <c>plotTikz</c> command.
    /// </summary>
    public override string? VisitPlotTikzCommand(Unipi.MppgParser.Grammar.MppgParser.PlotTikzCommandContext context) =>
        RenderCall(context);

    // A plot option is a name bound to a value, so it is tight: out="p.png", xlim=[0, 10].
    /// <summary>
    /// Writes one option of a plot command.
    /// </summary>
    public override string? VisitPlotOption(Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext context) =>
        $"{context.GetChild(0).GetText()}={Render(context.GetChild(2))}";

    /// <summary>
    /// Writes the interval a plot is drawn over.
    /// </summary>
    public override string? VisitInterval(Unipi.MppgParser.Grammar.MppgParser.IntervalContext context) =>
        $"[{Render(context.expression(0))}, {Render(context.expression(1))}]";

    // Grouping brackets are tight like call parentheses: (x + y), not ( x + y ).
    /// <summary>
    /// Writes a bracketed function expression.
    /// </summary>
    public override string? VisitFunctionBrackets(Unipi.MppgParser.Grammar.MppgParser.FunctionBracketsContext context) =>
        $"({Render(context.functionExpression())})";

    /// <summary>
    /// Writes a bracketed number expression.
    /// </summary>
    public override string? VisitEncNumberBrackets(Unipi.MppgParser.Grammar.MppgParser.EncNumberBracketsContext context) =>
        $"({Render(context.numberExpression())})";

    /// <summary>
    /// Writes a chain of sum-level operations, with the grouping made explicit.
    /// </summary>
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

    /// <summary>
    /// Writes a sum-level operation with the scalar written first, as in 1 + f.
    /// </summary>
    public override string? VisitFunctionShiftMinMaxRev(Unipi.MppgParser.Grammar.MppgParser.FunctionShiftMinMaxRevContext context)
    {
        var left = context.numberExpression();
        var right = context.functionProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberExpression(left))} {context.op.Text} {RenderOperand(right, IsCompoundFunctionProduct(right))}";
    }

    /// <summary>
    /// Writes a chain of product-level operations, with the grouping made explicit.
    /// </summary>
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

    /// <summary>
    /// Writes a scalar multiplication with the scalar first, as in 2 * f.
    /// </summary>
    public override string? VisitFunctionScalarMulRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulRevContext context)
    {
        var left = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} * {Render(context.functionUnaryExpression())}";
    }

    /// <summary>
    /// Writes a deconvolution with a scalar written first.
    /// </summary>
    public override string? VisitFunctionScalarDeconvolutionRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDeconvolutionRevContext context)
    {
        var left = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} / {Render(context.functionUnaryExpression())}";
    }

    /// <summary>
    /// Writes a composition between two scalars.
    /// </summary>
    public override string? VisitFunctionScalarScalarComposition(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarScalarCompositionContext context)
    {
        var left = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} comp {Render(context.numberUnaryExpression())}";
    }

    /// <summary>
    /// Writes a composition with the scalar written first.
    /// </summary>
    public override string? VisitFunctionScalarCompositionRev(Unipi.MppgParser.Grammar.MppgParser.FunctionScalarCompositionRevContext context)
    {
        var left = context.numberProductExpression();

        return $"{RenderOperand(left, IsCompoundNumberProduct(left))} comp {Render(context.functionUnaryExpression())}";
    }

    /// <summary>
    /// Writes a <c>ratency</c> call.
    /// </summary>
    public override string? VisitRateLatency(Unipi.MppgParser.Grammar.MppgParser.RateLatencyContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>bucket</c> call.
    /// </summary>
    public override string? VisitTokenBucket(Unipi.MppgParser.Grammar.MppgParser.TokenBucketContext context) => RenderCall(context);

    /// <summary>
    /// Writes an <c>affine</c> call.
    /// </summary>
    public override string? VisitAffineFunction(Unipi.MppgParser.Grammar.MppgParser.AffineFunctionContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>step</c> call.
    /// </summary>
    public override string? VisitStepFunction(Unipi.MppgParser.Grammar.MppgParser.StepFunctionContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>stair</c> call.
    /// </summary>
    public override string? VisitStairFunction(Unipi.MppgParser.Grammar.MppgParser.StairFunctionContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>delay</c> call.
    /// </summary>
    public override string? VisitDelayFunction(Unipi.MppgParser.Grammar.MppgParser.DelayFunctionContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>star</c> call, which is also spelled <c>subaddclosure</c>.
    /// </summary>
    public override string? VisitFunctionSubadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSubadditiveClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>superaddclosure</c> call.
    /// </summary>
    public override string? VisitFunctionSuperadditiveClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionSuperadditiveClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>hShift</c> call.
    /// </summary>
    public override string? VisitFunctionHShift(Unipi.MppgParser.Grammar.MppgParser.FunctionHShiftContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>vShift</c> call.
    /// </summary>
    public override string? VisitFunctionVShift(Unipi.MppgParser.Grammar.MppgParser.FunctionVShiftContext context) => RenderCall(context);

    /// <summary>
    /// Writes an <c>inv</c> call, which is also spelled <c>low_inv</c>.
    /// </summary>
    public override string? VisitFunctionLowerPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionLowerPseudoInverseContext context) => RenderCall(context);

    /// <summary>
    /// Writes an <c>up_inv</c> call.
    /// </summary>
    public override string? VisitFunctionUpperPseudoInverse(Unipi.MppgParser.Grammar.MppgParser.FunctionUpperPseudoInverseContext context) => RenderCall(context);

    /// <summary>
    /// Writes an <c>upclosure</c> call.
    /// </summary>
    public override string? VisitFunctionUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>nnupclosure</c> call.
    /// </summary>
    public override string? VisitFunctionNonNegativeUpNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>lowclosure</c> call.
    /// </summary>
    public override string? VisitFunctionLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonDecreasingClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>nnlowclosure</c> call.
    /// </summary>
    public override string? VisitFunctionNonNegativeLowNonDecreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionNonNegativeLowNonDecreasingClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes an <c>upnoninc</c>/<c>upnonincclosure</c> call.
    /// </summary>
    public override string? VisitFunctionUpNonIncreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionUpNonIncreasingClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>lownoninc</c>/<c>lownonincclosure</c> call.
    /// </summary>
    public override string? VisitFunctionLowNonIncreasingClosure(Unipi.MppgParser.Grammar.MppgParser.FunctionLowNonIncreasingClosureContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>left-ext</c> call.
    /// </summary>
    public override string? VisitFunctionLeftExt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftExtContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>right-ext</c> call.
    /// </summary>
    public override string? VisitFunctionRightExt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightExtContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>floor</c> call applied to a curve.
    /// </summary>
    public override string? VisitFunctionFloor(Unipi.MppgParser.Grammar.MppgParser.FunctionFloorContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>ceil</c> call applied to a curve.
    /// </summary>
    public override string? VisitFunctionCeil(Unipi.MppgParser.Grammar.MppgParser.FunctionCeilContext context) => RenderCall(context);

    /// <summary>
    /// Writes a sampling, as in f(3).
    /// </summary>
    public override string? VisitFunctionValueAt(Unipi.MppgParser.Grammar.MppgParser.FunctionValueAtContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>hDev</c> call.
    /// </summary>
    public override string? VisitFunctionHorizontalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionHorizontalDeviationContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>vDev</c> call.
    /// </summary>
    public override string? VisitFunctionVerticalDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionVerticalDeviationContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>zDev</c> call.
    /// </summary>
    public override string? VisitFunctionZDeviation(Unipi.MppgParser.Grammar.MppgParser.FunctionZDeviationContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>floor</c> call applied to a number.
    /// </summary>
    public override string? VisitEncNumberFloor(Unipi.MppgParser.Grammar.MppgParser.EncNumberFloorContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>ceil</c> call applied to a number.
    /// </summary>
    public override string? VisitEncNumberCeil(Unipi.MppgParser.Grammar.MppgParser.EncNumberCeilContext context) => RenderCall(context);

    /// <summary>
    /// Writes an <c>abs</c> call.
    /// </summary>
    public override string? VisitEncNumberAbs(Unipi.MppgParser.Grammar.MppgParser.EncNumberAbsContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>pow</c> call.
    /// </summary>
    public override string? VisitEncNumberPow(Unipi.MppgParser.Grammar.MppgParser.EncNumberPowContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>gcd</c> call.
    /// </summary>
    public override string? VisitEncNumberGcd(Unipi.MppgParser.Grammar.MppgParser.EncNumberGcdContext context) => RenderCall(context);

    /// <summary>
    /// Writes a <c>lcm</c> call.
    /// </summary>
    public override string? VisitEncNumberLcm(Unipi.MppgParser.Grammar.MppgParser.EncNumberLcmContext context) => RenderCall(context);

    /// <summary>
    /// Writes a left limit.
    /// </summary>
    public override string? VisitFunctionLeftLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        var name = context.functionName().GetText();
        var arg = Render(context.numberExpression());
        var tilde = context.ChildCount == 6 ? "~" : "";

        return $"{name}({arg}{tilde}-)";
    }

    /// <summary>
    /// Writes a right limit.
    /// </summary>
    public override string? VisitFunctionRightLimitAt(Unipi.MppgParser.Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        var name = context.functionName().GetText();
        var arg = Render(context.numberExpression());
        var tilde = context.ChildCount == 6 ? "~" : "";

        return $"{name}({arg}{tilde}+)";
    }

    /// <summary>
    /// Writes a <c>uaf</c> call, i.e. the sequence it is given.
    /// </summary>
    public override string? VisitUltimatelyAffineFunction(Unipi.MppgParser.Grammar.MppgParser.UltimatelyAffineFunctionContext context) =>
        $"uaf({Render(context.sequence())})";

    // An endpoint is a pair, so it follows the comma of an argument list: (0, -3).
    /// <summary>
    /// Writes an endpoint of a segment.
    /// </summary>
    public override string? VisitEndpoint(Unipi.MppgParser.Grammar.MppgParser.EndpointContext context) =>
        $"({Render(context.numberExpression(0))}, {Render(context.numberExpression(1))})";

    // Brackets are tight against the endpoints, and the slope between them is spaced as an operator:
    // [(0, -3)], [(0, -3) 1 (1, -2)[, ](0, -3) (1, -2)].
    /// <summary>
    /// Writes a point of a sequence.
    /// </summary>
    public override string? VisitPoint(Unipi.MppgParser.Grammar.MppgParser.PointContext context) =>
        $"[{Render(context.endpoint())}]";

    /// <summary>
    /// Writes a segment including neither endpoint.
    /// </summary>
    public override string? VisitSegmentLeftOpenRightOpen(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftOpenRightOpenContext context) =>
        RenderSegment("]", context, "[");

    /// <summary>
    /// Writes a segment including its right endpoint alone.
    /// </summary>
    public override string? VisitSegmentLeftOpenRightClosed(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftOpenRightClosedContext context) =>
        RenderSegment("]", context, "]");

    /// <summary>
    /// Writes a segment including its left endpoint alone.
    /// </summary>
    public override string? VisitSegmentLeftClosedRightOpen(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftClosedRightOpenContext context) =>
        RenderSegment("[", context, "[");

    /// <summary>
    /// Writes a segment including both its endpoints.
    /// </summary>
    public override string? VisitSegmentLeftClosedRightClosed(Unipi.MppgParser.Grammar.MppgParser.SegmentLeftClosedRightClosedContext context) =>
        RenderSegment("[", context, "]");

    /// <summary>
    /// Writes a <c>upp</c> call, i.e. the transient and periodic parts it is given.
    /// </summary>
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
            parts.Add(Render(increment.numberExpression()));
            var length = increment.periodLenght();
            if (length is not null)
                parts.Add(Render(length.numberExpression()));
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

    /// <summary>
    /// Renders a segment as <c>[start slope end[</c>, with the given brackets, or as
    /// <c>[start end[</c> when the slope is left to be computed.
    /// </summary>
    private string RenderSegment(string open, Antlr4.Runtime.ParserRuleContext context, string close)
    {
        var endpoints = context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.EndpointContext>();
        var slope = context.GetRuleContext<Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext>(0);
        var middle = slope is null ? "" : $"{Render(slope)} ";

        return $"{open}{Render(endpoints[0])} {middle}{Render(endpoints[1])}{close}";
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
