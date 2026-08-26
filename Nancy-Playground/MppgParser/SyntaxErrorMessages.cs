using Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// The user-facing message of a syntax error, written in the terms of the script rather than of the grammar.
/// What no matcher recognises keeps the message of ANTLR, so a message is never invented for an error that is not understood.
/// </summary>
/// <remarks>
/// A matcher reads the structure of the error rather than the text of its message: two of the shapes ANTLR reports carry no exception at all, being the recoveries of [Parr13] §9.3, and the rule being parsed, which says the most about what the user was writing, never reaches the message.
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
        new KeywordOfALaterVersionMatcher(),
        new AssignmentOperatorMatcher(),
        new MissingTokenMatcher(),
        new WrongNumberOfArgumentsMatcher(),
        new AfterTheEndOfTheStatementMatcher(),
        new IncompleteExpressionMatcher(),
        new UnknownVariableMatcher(),
        new ScalarCallArgumentsMatcher(),
        new IntervalEndMatcher(),
        new AssertionComparisonMatcher(),
        new WrongKindOfVariableMatcher(),
        new StatementCannotStartMatcher(),
        new PlotOptionValueMatcher(),
        new PlotArgumentMatcher(),
        new SamplingArgumentsMatcher(),
        new MissingOperatorMatcher()
    ];

    /// <summary>
    /// What to say about an error none of them recognised, which is what was expected put in words.
    /// </summary>
    /// <remarks>
    /// A stage of its own rather than a matcher among the others: it knows nothing about the mistake, only what could have stood there, so anything that does know is asked first.
    /// Held apart is also what keeps it from overlapping every matcher that reads an expected set.
    /// </remarks>
    internal static readonly IErrorMatcher<ParserError> ExpectedInWords = new SomethingElseWasExpectedMatcher();

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
        ParserError parserError => FirstMatch(parserError, ParserMatchers) ?? InWords(parserError),
        LexerError lexerError => FirstMatch(lexerError, LexerMatchers),
        _ => null
    };

    private static RewrittenMessage? InWords(ParserError error)
        => ExpectedInWords.Recognises(error)
            ? ExpectedInWords.Write(error) with { WrittenBy = ExpectedInWords.Name }
            : null;

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
