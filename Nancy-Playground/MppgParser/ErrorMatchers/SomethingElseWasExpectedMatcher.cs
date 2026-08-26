namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A token that cannot stand where it does, named together with what could have stood there instead.
/// </summary>
/// <remarks>
/// This is the shape ANTLR reports by listing the tokens it would have accepted, which runs to forty-odd for an expression, so what is expected is put in words instead.
/// The messages it writes are the ones of [Parr13] §9.1.
/// It is the stage below the matchers, so it says nothing about the mistake and needs no guard against them: whatever knows more has already spoken.
/// </remarks>
internal sealed class SomethingElseWasExpectedMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "something else was expected";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Tokens.Offending is { } token
            // the end of the statement is what follows a statement read whole, which reads better said that way
            && !error.Expected.AreOnlyTheEndOfTheStatement
            // a token that is itself among what was expected explains nothing named that way: in h := ((f + 1) (f - 1)) the '(' can open an expression, and the mistake is the operator left out before it
            && !error.Expected.Types.Contains(token.Type)
            && error.Expected.InWords is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        // the end of the line is where the line stops, which reads better than the newline it is quoted as
        => TokenFacts.EndsTheLine(error.Tokens.Offending)
            ? new($"the line ends where {error.Expected.InWords} was expected")
            : new($"unexpected '{error.Tokens.Offending!.Text}', {error.Expected.InWords} was expected instead");
}
