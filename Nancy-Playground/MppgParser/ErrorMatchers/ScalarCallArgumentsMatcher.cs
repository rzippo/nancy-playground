namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A scalar operation given the wrong number of arguments, as <c>pow(2)</c> and <c>abs(2, 3)</c> are.
/// </summary>
/// <remarks>
/// The constructors fail inside their own rule, which <see cref="WrongNumberOfArgumentsMatcher"/> reads, where a scalar call fails out in the expression with nothing on the rule stack naming it.
/// The name is read from the tokens instead, as the call the offending token stands inside.
/// </remarks>
internal sealed class ScalarCallArgumentsMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "wrong number of arguments to a scalar operation";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Recovery == ParserRecovery.None
            // an assertion is written like a call and is not one: what it takes is a comparison, not a list of arguments
            && !error.Rule.IsInside("assertion")
            && error.Tokens.Offending?.Text is ")" or ","
            && error.Expected.Count > 1
            && CallArity.Says(error.EnclosingCall?.Text) is not null
            // the arity of a call the version in force does not have is beside the point
            && error.KeywordOfALaterVersion is null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error) => new(CallArity.Says(error.EnclosingCall!.Text)!);
}
