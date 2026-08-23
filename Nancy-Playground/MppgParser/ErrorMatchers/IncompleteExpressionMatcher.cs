using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// An expression that runs out before it is whole, the line ending where an operand or a closing bracket was still to come.
/// </summary>
internal sealed class IncompleteExpressionMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "incomplete expression";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Exception is NoViableAltException
            && TokenFacts.EndsTheLine(error.Tokens.Offending)
            && error.Rule.IsInside("expression");

    /// <summary>
    /// It says that the expression is incomplete and no more: what is missing differs between <c>1 +</c>, wanting an operand, and <c>(1 + 2</c>, wanting a bracket, and the parser reports both the same way, from the expression rather than from the bracket it was inside.
    /// </summary>
    public RewrittenMessage Write(ParserError error) => new("the expression is incomplete");
}
