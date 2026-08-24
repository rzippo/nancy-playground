namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A curve sampled at more than one point, as <c>f(3, 4)</c> is.
/// </summary>
/// <remarks>
/// A sampling is written like a call, so a comma inside it reads as a second argument, and the name in front of the bracket is a variable rather than a keyword.
/// </remarks>
internal sealed class SamplingArgumentsMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "sampling at more than one point";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Recovery == ParserRecovery.None
            && error.Tokens.Offending?.Text == ","
            && error.EnclosingCall is { } call
            && call.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            && error.DeclaredVariables.TryGetValue(call.Text, out var kind)
            && kind == Unipi.MppgParser.Grammar.MppgParser.VariableType.Function;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"'{error.EnclosingCall!.Text}' is sampled at one point, so it takes one argument");
}
