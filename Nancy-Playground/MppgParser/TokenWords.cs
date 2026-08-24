namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// How a token is named to a reader, where the vocabulary of the grammar names it for a grammar author.
/// </summary>
/// <remarks>
/// A literal keeps its spelling, quoted, which is what the reader typed or should have.
/// A token named after its rule, e.g. NUMBER_ABS_LITERAL, says nothing to them, so the few that turn up in a message are given words.
/// </remarks>
internal static class TokenWords
{
    private static readonly IReadOnlyDictionary<string, string> Words = new Dictionary<string, string>
    {
        ["IDENTIFIER"] = "a name",
        ["NUMBER_ABS_LITERAL"] = "a number",
        ["STRING_LITERAL"] = "a string",
        ["NEW_LINE"] = "the end of the line",
        ["EOF"] = "the end of the input",
        ["INLINABLE_COMMENT"] = "a comment",
        ["DIRECTIVE_START"] = "a directive",
        ["SYNTAX_DIRECTIVE"] = "a '#!syntax version' directive"
    };

    /// <summary>
    /// What to call the token the vocabulary spells <paramref name="vocabularyName"/>.
    /// </summary>
    public static string Of(string vocabularyName)
        => Words.TryGetValue(vocabularyName, out var words)
            ? words
            : $"'{vocabularyName.Trim('\'')}'";
}
