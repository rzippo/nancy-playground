namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A variable used where the other kind belongs, as a number given to <c>plot</c> is, or a number sampled as <c>x(3)</c>.
/// </summary>
/// <remarks>
/// The grammar tells the kinds apart with a predicate, which prunes the alternative rather than failing in it, so the parser can only say that no alternative fitted.
/// What it cannot say is read from the variables declared so far, which name the kind the variable was given.
/// </remarks>
internal sealed class WrongKindOfVariableMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "variable of the wrong kind";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => KindOf(error) is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
    {
        var name = error.Tokens.Offending!.Text;
        var kind = KindOf(error) == Unipi.MppgParser.Grammar.MppgParser.VariableType.Number ? "a number" : "a function";

        if (error.Tokens.Next?.Text == "(")
            return new($"'{name}' is {kind}, and only a function can be sampled");

        // the command it was given to says what it takes, where the rule stack names one
        var command = error.Rule.IsInside("plotTikzCommand") ? "plotTikz"
            : error.Rule.IsInside("plotCommand") ? "plot"
            : null;

        return command is null
            ? new($"'{name}' is {kind}, which cannot stand here")
            : new($"'{name}' is {kind}, '{command}' takes functions");
    }

    /// <summary>
    /// The kind the offending name was declared with, where it is a name that was declared and the parser had no alternative for it.
    /// </summary>
    private static Unipi.MppgParser.Grammar.MppgParser.VariableType? KindOf(ParserError error)
    {
        if (error.Recovery != ParserRecovery.None
            || error.Tokens.Offending is not { } token
            || token.Type != Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            || !error.DeclaredVariables.TryGetValue(token.Text, out var kind))
            return null;

        return kind;
    }
}
