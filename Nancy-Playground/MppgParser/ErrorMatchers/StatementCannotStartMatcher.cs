namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A token that opens a line where no statement can begin with it, as a stray <c>)</c> does.
/// </summary>
/// <remarks>
/// The grammar lets a line be empty, so the parser expects the end of the line rather than the start of a statement, and nothing in the expected set says what could have opened one.
/// The other way round from <see cref="AfterTheEndOfTheStatementMatcher"/>, which reads the same expected set where something does stand before the token on its line.
/// </remarks>
internal sealed class StatementCannotStartMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "statement cannot start here";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Tokens.Offending is { } token
            && !TokenFacts.EndsTheLine(token)
            && (error.Tokens.Previous is null || TokenFacts.EndsTheLine(error.Tokens.Previous))
            && error.Expected.AreOnlyTheEndOfTheStatement;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"a statement cannot start with '{error.Tokens.Offending!.Text}'");
}
