using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// An argument list closed too early, or carried on too long, which the parser meets as a comma where it wanted the closing bracket, or the other way round.
/// </summary>
/// <remarks>
/// The scalar operations are not recognised here: <c>pow(2)</c> fails further out, in the expression, where what was expected is the whole start set of one.
/// </remarks>
internal sealed class WrongNumberOfArgumentsMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "wrong number of arguments";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => Complaint(error) is not null;

    /// <summary>
    /// The name is the first token of the rule being parsed, the rule names of the grammar being internal: <c>tokenBucket</c> for what a user writes as <c>bucket</c>.
    /// </summary>
    public RewrittenMessage Write(ParserError error)
        => new($"'{error.Rule.StartToken!.Text}' {Complaint(error)}");

    /// <summary>
    /// What is wrong with the list, or null where the error is not about one.
    /// </summary>
    private static string? Complaint(ParserError error)
    {
        if (error.Exception is not InputMismatchException
            || error.Expected.Count != 1
            || error.Tokens.Offending is not { } token
            || !TokenFacts.IsKeywordSpelledLikeAName(error.Rule.StartToken))
            return null;

        return (error.Expected.Only, token.Text) switch
        {
            (",", ")") => "needs another argument",
            (")", ",") => "takes no more arguments",
            _ => null
        };
    }
}
