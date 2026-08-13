using Antlr4.Runtime.Tree;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Utility;

/// <summary>
/// Finds the one shape whose grouping the syntax cannot settle for the reader: a curve divided by a scalar, followed by more scalar factors, as in <c>f / 1/2</c> or <c>f / x * y</c>.
/// </summary>
/// <remarks>
/// The scalar operands of the product operators bind one at a time and fold left to right, exactly as they do between scalars, so <c>f / 1/2</c> is <c>(f / 1) / 2</c>.
/// RTaW instead reads the divisor as one whole scalar expression when it starts with a number, making the same text <c>f / (1 / 2)</c>, and folds left when it starts with a variable.
/// That split is deliberately not reproduced, because it would make the grouping depend on the type of the other operand.
/// Neither reading is wrong and both are silent, so the parser says so wherever the two differ.
/// </remarks>
public static class ScalarDivisionGrouping
{
    /// <summary>
    /// The warnings for every ambiguous scalar division chain under <paramref name="tree"/>, in the order they appear.
    /// </summary>
    public static IReadOnlyList<string> WarningsFor(IParseTree? tree)
    {
        if (tree is null)
            return [];

        List<string> warnings = [];
        Collect(tree, warnings);
        return warnings;
    }

    private static void Collect(IParseTree tree, List<string> warnings)
    {
        if (tree is Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext chain
            && IsAmbiguous(chain))
        {
            warnings.Add(WarningFor(chain));
        }

        for (var i = 0; i < tree.ChildCount; i++)
            Collect(tree.GetChild(i), warnings);
    }

    /// <summary>
    /// True when the two tools group the chain differently, which needs all three of these.
    /// The first scalar operator is a division: with a multiplication first, <c>(f * a) / b</c> and <c>f * (a / b)</c> agree.
    /// Another scalar factor follows it: with a non-scalar operator next, such as a convolution, there is no scalar chain to absorb in the first place.
    /// The divisor starts with a number: that is the token RTaW keys on, and where it starts with a variable or a sampled value it folds left exactly as we do, so there is nothing to warn about.
    /// </summary>
    private static bool IsAmbiguous(Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext chain)
    {
        var suffixes = chain.functionProductSuffix();
        return suffixes.Length >= 2
            && suffixes[0] is Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivSuffixContext divisor
            && IsScalarFactor(suffixes[1])
            && StartsWithNumberLiteral(divisor);
    }

    private static bool IsScalarFactor(Unipi.MppgParser.Grammar.MppgParser.FunctionProductSuffixContext suffix) =>
        suffix is Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivSuffixContext
            or Unipi.MppgParser.Grammar.MppgParser.FunctionScalarMulSuffixContext;

    /// <summary>
    /// Whether the operand of the suffix begins with a number, once any signs in front of it are passed.
    /// </summary>
    private static bool StartsWithNumberLiteral(
        Unipi.MppgParser.Grammar.MppgParser.FunctionScalarDivSuffixContext suffix)
    {
        foreach (var type in LeadingTokenTypes(suffix.numberUnaryExpression()))
        {
            if (type == Unipi.MppgParser.Grammar.MppgLexer.PLUS
                || type == Unipi.MppgParser.Grammar.MppgLexer.MINUS)
                continue;

            return type == Unipi.MppgParser.Grammar.MppgLexer.NUMBER_ABS_LITERAL;
        }

        return false;
    }

    private static IEnumerable<int> LeadingTokenTypes(IParseTree tree)
    {
        if (tree is ITerminalNode terminal)
        {
            yield return terminal.Symbol.Type;
            yield break;
        }

        for (var i = 0; i < tree.ChildCount; i++)
            foreach (var type in LeadingTokenTypes(tree.GetChild(i)))
                yield return type;
    }

    private static string WarningFor(Unipi.MppgParser.Grammar.MppgParser.FunctionProductChainContext chain)
    {
        var text = chain.GetJoinedText();
        var divisor = chain.functionProductSuffix()[0].GetJoinedText().TrimStart('/', ' ');

        return $"WARNING: in \"{text}\", scalar factors fold left to right, so the divisor is {divisor} alone "
            + "and the rest applies to the result. RTaW reads a divisor that starts with a number as one "
            + "whole value, so it computes a different curve here. Parenthesise the divisor to say which "
            + "one you mean.";
    }
}
