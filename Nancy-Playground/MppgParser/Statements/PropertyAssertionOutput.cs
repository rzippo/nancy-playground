using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The outcome of a property assertion, i.e. whether the property held.
/// </summary>
public class PropertyAssertionOutput : StatementOutput
{
    /// <summary>
    /// The expression the property was tested against.
    /// </summary>
    public required IExpression Operand { get; init; }

    /// <summary>
    /// The time it took to compute the value the property was read from.
    /// </summary>
    public required TimeSpan Time { get; init; }

    /// <summary>
    /// The result of the property test.
    /// </summary>
    public required bool Result { get; init; }
}
