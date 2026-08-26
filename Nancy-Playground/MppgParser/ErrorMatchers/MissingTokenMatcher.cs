namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A token the parser had to invent to carry on, which is a bracket left open, or a comma left out between two arguments.
/// </summary>
/// <remarks>
/// The parser reports this one by recovering rather than by raising, and an extraneous token comes the same way, so the two are told apart by the recovery the parser recorded.
/// What was expected is read from its own expected set.
/// </remarks>
internal sealed class MissingTokenMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "token missing";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Recovery == ParserRecovery.MissingToken
            && error.Expected.Count == 1;

    /// <summary>
    /// Where the token it stopped at ends the line, saying so reads better than quoting a newline, which is what the message of ANTLR does.
    /// The token itself is one the parser invented to carry on, which [Parr13] §9.3 calls single-token insertion.
    /// </summary>
    public RewrittenMessage Write(ParserError error)
        => new(TokenFacts.EndsTheLine(error.Tokens.Offending)
            ? $"a '{error.Expected.Only}' is missing at the end of the line"
            : $"a '{error.Expected.Only}' is missing before '{error.Tokens.Offending?.Text}'");
}
