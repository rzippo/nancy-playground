namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A name given to a plot that is neither one of its options nor a function it can draw.
/// </summary>
/// <remarks>
/// An argument of a plot is one or the other, so a name that is neither cannot be reported as an unknown variable without guessing which was meant.
/// The equals sign after it is what says: with one, the name was written as an option, and without one, as a function.
/// </remarks>
internal sealed class PlotArgumentMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "name a plot cannot take";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Rule.IsInside("plotArg")
            && error.Tokens.Offending is { } token
            && token.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            && !error.IsDeclared(token.Text);

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => error.Tokens.Next?.Text == "="
            ? new($"'{error.Tokens.Offending!.Text}' is not an option of a plot")
            : new($"'{error.Tokens.Offending!.Text}' is neither a declared function nor an option of a plot");
}
