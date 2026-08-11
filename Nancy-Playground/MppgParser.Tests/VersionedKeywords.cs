using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// Keywords that were not part of the syntax from the start, and the version that introduced each.
/// </summary>
/// <remarks>
/// Test data only: the gating itself is defined by the predicated lexer rules of Mppg.g4.
/// Listing the keywords here lets the version tests cover keywords added later without being edited,
/// and lets them check that the grammar and this list agree.
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
    /// True if <paramref name="keyword"/> is a keyword in <paramref name="version"/>.
    /// Names not listed in <see cref="IntroducedIn"/> are keywords in every version.
    /// </summary>
    public static bool IsKeywordIn(string keyword, SyntaxVersion version)
        => !IntroducedIn.TryGetValue(keyword, out var introduced) || version >= introduced;

    private static Dictionary<string, int>? _keywordTokenTypes;

    /// <summary>
    /// Token type of each keyword in <see cref="IntroducedIn"/>, obtained by lexing the keyword itself.
    /// Entries that do not lex as a keyword are left out.
    /// </summary>
    /// <remarks>
    /// Resolved this way, rather than from hardcoded token numbers, because numbering shifts whenever the
    /// grammar changes.
    /// </remarks>
    public static Dictionary<string, int> KeywordTokenTypes()
    {
        if (_keywordTokenTypes is not null)
            return _keywordTokenTypes;

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var keyword in IntroducedIn.Keys)
        {
            var tokenType = LexAsSingleToken(keyword);
            if (tokenType is not null && tokenType != Unipi.MppgParser.Grammar.MppgLexer.VARIABLE_NAME)
                map[keyword] = tokenType.Value;
        }

        _keywordTokenTypes = map;
        return map;
    }

    /// <summary>
    /// Type of the single token <paramref name="text"/> lexes to, or null if it does not lex to exactly one.
    /// </summary>
    private static int? LexAsSingleToken(string text)
    {
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(CharStreams.fromString(text));
        lexer.RemoveErrorListeners();

        var token = lexer.NextToken();
        if (token.Type == TokenConstants.EOF || token.StopIndex != text.Length - 1)
            return null;

        return token.Type;
    }
}
