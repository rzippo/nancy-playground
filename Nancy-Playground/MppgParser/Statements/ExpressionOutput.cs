using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser;

public class ExpressionOutput : StatementOutput
{
    /// <summary>
    /// The expression that was assigned.
    /// </summary>
    public required IExpression Expression { get; init; }

    /// <summary>
    /// The time it took to compute the result to be assigned.
    /// </summary>
    /// <remarks>
    /// May vary depending on whether the expression was fully computed before assignment or not. 
    /// </remarks>
    public required TimeSpan Time { get; init; }
}