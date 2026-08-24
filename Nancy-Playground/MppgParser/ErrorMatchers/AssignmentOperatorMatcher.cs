namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// An assignment written with an equals sign, as <c>T4 = 60</c> is.
/// </summary>
/// <remarks>
/// One of the few mistypings worth naming: a line that opens with a name and an equals sign is an assignment and can be nothing else, where a name and an expression with nothing between them says only that something is missing.
/// </remarks>
internal sealed class AssignmentOperatorMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "assignment written with an equals sign";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => error.IsAssignmentWrittenWithAnEquals;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"an assignment to '{error.NameAssignedWithAnEquals!.Text}' is written with ':=', not with '='");
}
