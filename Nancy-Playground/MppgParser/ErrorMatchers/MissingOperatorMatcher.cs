namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// Two expressions written side by side with nothing between them, as <c>((f + 1) (f - 1))</c> is.
/// </summary>
/// <remarks>
/// The syntax has no juxtaposition, so a bracket opening where one has just closed is an operator left out rather than anything else.
/// This is the case the naming cannot describe: the token it stopped at can open an expression, which is exactly why it reads as one too many.
/// </remarks>
internal sealed class MissingOperatorMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "operator missing between two expressions";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Recovery == ParserRecovery.None
            && error.Tokens.Offending?.Text == "("
            && error.Tokens.Previous?.Text == ")"
            && error.Rule.IsInside("expression");

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new("an operator is missing between the two expressions");
}
