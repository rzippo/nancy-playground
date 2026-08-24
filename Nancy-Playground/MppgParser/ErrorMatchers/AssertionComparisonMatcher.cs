namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// An assertion of one expression, where it compares two, as <c>assert(f)</c> is.
/// </summary>
/// <remarks>
/// The parser stops where the comparison should stand and reports that an expression was expected, an expression being what follows the operator: the rule says that an assertion is what is being written, which is what the reader needs told.
/// </remarks>
internal sealed class AssertionComparisonMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "assertion without a comparison";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Rule.IsInside("assertion")
            && error.Tokens.Offending?.Text is ")"
            && error.Recovery == ParserRecovery.None;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new("'assert' takes a comparison between two expressions",
            "The operators it compares with are '=', '!=', '<', '<=', '>' and '>='.");
}
