namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A token that cannot stand where it does, named together with what could have stood there instead.
/// </summary>
/// <remarks>
/// This is the shape ANTLR reports by listing the tokens it would have accepted, which runs to forty-odd for an expression, so what is expected is put in words instead.
/// It claims neither an unknown name nor a keyword written as one, both of which have more to say about the token than that it does not fit.
/// </remarks>
internal sealed class SomethingElseWasExpectedMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "something else was expected";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        // a token the parser dropped is one that could not stand there either, so it is named the same way
        => error.Recovery != ParserRecovery.MissingToken
            && error.Tokens.Offending is { } token
            && !TokenFacts.EndsTheLine(token)
            // a name is left to the matchers that know what a name can be wrong about
            && token.Type != Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            && !TokenFacts.IsKeywordSpelledLikeAName(token)
            && error.KeywordBeingNamed is null
            // the end of the statement is what follows a statement read whole, which reads better said that way
            && !error.Expected.AreOnlyTheEndOfTheStatement
            // a token that is itself among what was expected explains nothing named that way: in h := ((f + 1) (f - 1)) the '(' can open an expression, and the mistake is the operator left out before it
            && !error.Expected.Types.Contains(token.Type)
            && error.Expected.Count > 1
            && error.Expected.InWords is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"unexpected '{error.Tokens.Offending!.Text}', {error.Expected.InWords} was expected instead");
}
