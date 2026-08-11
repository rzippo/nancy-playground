using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// How the keywords of <see cref="VersionedKeywords.IntroducedIn"/> lex, to check that the list and the
/// grammar agree on what is a keyword.
/// </summary>
public static class KeywordLexing
{
    private static Dictionary<string, int>? _keywordTokenTypes;

    /// <summary>
    /// Token type of each keyword in <see cref="VersionedKeywords.IntroducedIn"/>, obtained by lexing the
    /// keyword itself. Entries that do not lex as a keyword are left out.
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
        foreach (var keyword in VersionedKeywords.IntroducedIn.Keys)
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
