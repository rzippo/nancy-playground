namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A statement read whole, with something after it that cannot be there: a bracket too many, or a second statement written on the same line.
/// </summary>
/// <remarks>
/// Recognised by what would have fitted, the end of the statement and nothing else, which says the same whether the parser reported an extraneous token or a mismatched one.
/// Only where the token follows the statement on its line: one that opens a line of its own is where a statement was to begin, and saying it comes after one would point at the wrong place.
/// </remarks>
internal sealed class AfterTheEndOfTheStatementMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "after the end of the statement";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Tokens.Offending is { } token
            && !TokenFacts.EndsTheLine(token)
            // nothing before it on the line means no statement was read, so nothing follows one
            && error.Tokens.Previous is not null
            && !TokenFacts.EndsTheLine(error.Tokens.Previous)
            && error.Expected.AreOnlyTheEndOfTheStatement;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"unexpected '{error.Tokens.Offending!.Text}' after the end of the statement");
}
