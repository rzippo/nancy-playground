using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The output of <c>printExpression</c>, which shows an expression rather than the value it computes.
/// It carries the expression, so that a formatter can write it in the notation of its output mode.
/// </summary>
public class PrintExpressionOutput : StatementOutput
{
    /// <summary>
    /// The expression that was printed, as it was written rather than as it computes.
    /// </summary>
    public required IExpression Expression { get; init; }
}
