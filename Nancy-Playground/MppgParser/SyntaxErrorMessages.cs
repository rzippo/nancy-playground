using Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// The user-facing message of a syntax error, written in the terms of the script rather than of the grammar.
/// What no matcher recognises keeps the message of ANTLR, so a message is never invented for an error that is not understood.
/// </summary>
/// <remarks>
/// A matcher reads the structure of the error rather than the text of its message: two of the shapes ANTLR reports carry no exception at all, and the rule being parsed, which says the most about what the user was writing, never reaches the message.
/// Two matchers recognising the same error is a defect rather than a precedence to settle: the registries are read in order, and a test holds them to at most one match per error.
/// Each matcher is a file of its own under <c>ErrorMatchers</c>, and joins a registry below.
/// </remarks>
internal static class SyntaxErrorMessages
{
    /// <summary>
    /// The matchers of an error of the parser.
    /// </summary>
    internal static readonly IReadOnlyList<IErrorMatcher<ParserError>> ParserMatchers =
    [
        new KeywordUsedAsNameMatcher(),
        new MissingTokenMatcher(),
        new WrongNumberOfArgumentsMatcher(),
        new AfterTheEndOfTheStatementMatcher(),
        new IncompleteExpressionMatcher(),
        new UnknownVariableMatcher(),
        new SomethingElseWasExpectedMatcher()
    ];

    /// <summary>
    /// The matchers of an error of the lexer.
    /// </summary>
    internal static readonly IReadOnlyList<IErrorMatcher<LexerError>> LexerMatchers =
    [
        new StringNotClosedMatcher(),
        new CharacterInANameMatcher(),
        new UnsupportedCharacterMatcher()
    ];

    /// <summary>
    /// What to show for <paramref name="error"/>, or null to keep what it carries.
    /// </summary>
    /// <remarks>
    /// Dispatches on the kind, so that a matcher reading a rule cannot be handed an error of the lexer.
    /// </remarks>
    public static RewrittenMessage? Rewrite(ParseError error) => error switch
    {
        ParserError parserError => FirstMatch(parserError, ParserMatchers),
        LexerError lexerError => FirstMatch(lexerError, LexerMatchers),
        _ => null
    };

    private static RewrittenMessage? FirstMatch<TError>(TError error, IReadOnlyList<IErrorMatcher<TError>> matchers)
        where TError : ParseError
    {
        foreach (var matcher in matchers)
        {
            if (matcher.Recognises(error))
                return matcher.Write(error) with { WrittenBy = matcher.Name };
        }

        return null;
    }
}
