using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

/// <summary>
/// Writes a value, or the expression that produces it, in the notation of Nancy rather than of the
/// syntax: the C# that builds a value, and the operator names of Nancy for an expression.
/// It is what the modes about Nancy print, and what a print command for Nancy would print.
/// </summary>
public static class NancyOutput
{
    /// <summary>
    /// The computed value of <paramref name="expression"/>, as the C# that builds it.
    /// </summary>
    public static string OfValue(IExpression expression)
        => expression switch
        {
            CurveExpression curve => curve.Value.ToCodeString(),
            RationalExpression rational => rational.Value.ToPrettyString(),
            _ => throw new InvalidOperationException("The expression is neither a curve nor a rational.")
        };

    /// <summary>
    /// The expression itself, in the operator names of Nancy, e.g. <c>subadditiveClosure(f ⊗ g)</c>.
    /// </summary>
    public static string OfExpression(IExpression expression)
        => expression.ToUnicodeString();
}
