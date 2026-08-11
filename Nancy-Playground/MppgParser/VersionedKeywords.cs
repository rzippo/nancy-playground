using Antlr4.Runtime;
using Unipi.Nancy.Playground.MppgParser.Exceptions;

namespace Unipi.Nancy.Playground.MppgParser;

public static class VersionedKeywordExceptionExtensions
{
    /// <summary>
    /// The exception to report for a parse that failed, with the hint of <see cref="VersionedKeywords"/>
    /// if the input used a keyword of a later version as a name.
    /// Used by the parse entry points that bail on the first error, which have no error listener.
    /// </summary>
    public static Exception WithVersionedKeywordHint(this Exception exception, ITokenStream tokenStream)
    {
        if (tokenStream is BufferedTokenStream buffered)
        {
            buffered.Fill();
            if (VersionedKeywords.TryGetUsedAsNameHint(buffered.GetTokens()) is { } hint)
                return new SyntaxErrorException(hint, exception);
        }

        return exception;
    }
}

/// <summary>
/// Keywords that were not part of the syntax from the start, and the version that introduced each.
/// </summary>
/// <remarks>
/// The gating itself is defined by the predicated lexer rules of Mppg.g4, which this list mirrors,
/// so that a program using one of these names as a variable can be told why it no longer parses.
/// Adding a gated keyword to the grammar without listing it here fails a test of MppgParser.Tests.
/// </remarks>
public static class VersionedKeywords
{
    /// <summary>
    /// Keyword text mapped to the first version in which it is a keyword.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, SyntaxVersion> IntroducedIn =
        new Dictionary<string, SyntaxVersion>(StringComparer.Ordinal)
        {
            ["plotTikz"] = SyntaxVersion.V1_1,
            ["printExpression"] = SyntaxVersion.V1_1,
            ["subaddclosure"] = SyntaxVersion.V1_2,
            ["superaddclosure"] = SyntaxVersion.V1_2,
            ["lowclosure"] = SyntaxVersion.V1_2,
            ["nnlowclosure"] = SyntaxVersion.V1_2,
            ["floor"] = SyntaxVersion.V1_3,
            ["ceil"] = SyntaxVersion.V1_3,
        };

    /// <summary>
    /// The hint to add to a syntax error, if the tokens contain a versioned keyword used as a name.
    /// </summary>
    /// <param name="tokens">The tokens of the statement that failed to parse.</param>
    public static string? TryGetUsedAsNameHint(IEnumerable<IToken> tokens)
    {
        var (keyword, introducedIn) = FindUsedAsName(tokens);
        return keyword is null ? null : UsedAsNameHint(keyword, introducedIn);
    }

    /// <summary>
    /// The tokens of the line of <paramref name="offendingToken"/>, which is the statement that failed
    /// to parse: a program is parsed as a whole, so the rest of it is not what the error is about.
    /// </summary>
    public static IEnumerable<IToken> TokensOfLine(ITokenStream? tokenStream, IToken? offendingToken)
    {
        if (tokenStream is not BufferedTokenStream buffered || offendingToken is null)
            return [];

        return buffered.GetTokens().Where(token => token.Line == offendingToken.Line);
    }

    /// <summary>
    /// The first versioned keyword among <paramref name="tokens"/> that is used as a name rather than
    /// as the operator or command it spells, or (null, default) if there is none.
    /// </summary>
    private static (string? Keyword, SyntaxVersion IntroducedIn) FindUsedAsName(IEnumerable<IToken> tokens)
    {
        IToken? previous = null;
        foreach (var token in tokens)
        {
            if (previous is not null && IsUsedAsName(previous, token))
                return (previous.Text, IntroducedIn[previous.Text]);

            previous = token;
        }

        return previous is not null && IsUsedAsName(previous, null)
            ? (previous.Text, IntroducedIn[previous.Text])
            : (null, default);
    }

    /// <summary>
    /// True if <paramref name="token"/> is a versioned keyword that is not applied to an argument,
    /// which is how a program written before the keyword existed uses that name.
    /// </summary>
    private static bool IsUsedAsName(IToken token, IToken? next)
    {
        // a name lexed as a variable is one, whatever it spells: it is not a keyword of this version
        return token.Type != Unipi.MppgParser.Grammar.MppgLexer.VARIABLE_NAME
            && IntroducedIn.ContainsKey(token.Text)
            && next?.Text != "(";
    }

    private static string UsedAsNameHint(string keyword, SyntaxVersion introducedIn)
    {
        var lastVersionWithoutIt = introducedIn.Previous();
        var directive = lastVersionWithoutIt is null
            ? "declare an earlier syntax version"
            : $"declare '#!syntax version {lastVersionWithoutIt}'";

        return $"'{keyword}' is a keyword of the syntax from version {introducedIn} on, so it cannot be a name: "
            + $"to keep using it as one, {directive} before any other statement.";
    }
}
