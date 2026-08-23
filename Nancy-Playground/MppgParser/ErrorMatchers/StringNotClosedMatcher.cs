namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// The quote that opens a string the lexer never finds the end of.
/// </summary>
internal sealed class StringNotClosedMatcher : IErrorMatcher<LexerError>
{
    /// <inheritdoc/>
    public string Name => "string not closed";

    /// <inheritdoc/>
    public bool Recognises(LexerError error) => error.Character is ['"', ..];

    /// <inheritdoc/>
    public RewrittenMessage Write(LexerError error) => new("a string is not closed");
}
