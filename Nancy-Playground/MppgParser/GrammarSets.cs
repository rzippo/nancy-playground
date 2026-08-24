using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// The tokens each construct of the syntax can begin with, read from the grammar itself.
/// </summary>
/// <remarks>
/// A parser that stops mid-expression expects any of the forty-odd tokens an expression can open with, which is unreadable listed and says one word named.
/// The sets are computed from the ATN rather than written down, so that adding a keyword to a construct cannot leave the name behind, and a test holds each one to what the grammar says.
/// </remarks>
internal static class GrammarSets
{
    private static readonly Lazy<IReadOnlyList<(string Words, IntervalSet Tokens)>> Named = new(() =>
    [
        ("an expression", StartSetOf(Unipi.MppgParser.Grammar.MppgParser.RULE_expression)),
        ("a function", StartSetOf(Unipi.MppgParser.Grammar.MppgParser.RULE_functionExpression)),
        ("a number", StartSetOf(Unipi.MppgParser.Grammar.MppgParser.RULE_numberExpression)),
        ("a statement", StartSetOf(Unipi.MppgParser.Grammar.MppgParser.RULE_statement)),
        ("a comparison", StartSetOf(Unipi.MppgParser.Grammar.MppgParser.RULE_assertionOperator))
    ]);

    /// <summary>
    /// The tokens <paramref name="rule"/> can begin with.
    /// </summary>
    private static IntervalSet StartSetOf(int rule)
    {
        var atn = Unipi.MppgParser.Grammar.MppgParser._ATN;
        return atn.NextTokens(atn.ruleToStartState[rule]);
    }

    /// <summary>
    /// The construct <paramref name="expected"/> is the opening of, in words, or null where it is not one of them.
    /// </summary>
    /// <remarks>
    /// The expected set of an error is the start set of a construct less whatever the alternative already ruled out, so it is asked to cover the named set rather than to equal it.
    /// The most specific name wins, an expression covering both of the kinds it is made of.
    /// </remarks>
    public static string? Naming(IReadOnlyList<int> expected)
    {
        if (expected.Count < MinimumToName)
            return null;

        var tokens = new IntervalSet(expected.ToArray());
        foreach (var (words, named) in Named.Value)
        {
            if (Covers(tokens, named))
                return words;
        }

        return null;
    }

    /// <summary>
    /// A set small enough to spell is spelled, so naming starts above the size at which listing stops reading well.
    /// </summary>
    public const int MinimumToName = 4;

    /// <summary>
    /// The share of a named set that has to be expected for the name to be used.
    /// </summary>
    private const double Coverage = 0.8;

    private static bool Covers(IntervalSet expected, IntervalSet named)
    {
        var wanted = named.ToArray();
        if (wanted.Length == 0)
            return false;

        var found = wanted.Count(expected.Contains);
        return found >= wanted.Length * Coverage
            && expected.ToArray().All(token => named.Contains(token) || token == TokenConstants.EOF);
    }
}
