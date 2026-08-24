namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A name used where an expression is expected, which was never declared: a typo, or a use before the declaration, which this syntax does not allow.
/// </summary>
/// <remarks>
/// Scoped to an expression: the argument of a plot names an option as well as a variable, so a name unknown there is not necessarily a variable, and stays with the message of ANTLR.
/// </remarks>
internal sealed class UnknownVariableMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "unknown variable";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Tokens.Offending is { } token
            && token.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            && error.Rule.IsInside("expression")
            && !error.IsDeclared(token.Text)
            // a name the line opens an assignment with is not unknown, it is the name being assigned
            && !error.IsAssignmentWrittenWithAnEquals;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"'{error.Tokens.Offending!.Text}' is not a declared variable");
}
