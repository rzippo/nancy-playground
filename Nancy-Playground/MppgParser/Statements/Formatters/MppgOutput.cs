using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

/// <summary>
/// Writes a value, or the expression that produces it, as MPPG source text, which is the notation the
/// playground prints unless an output mode asks for another one.
/// </summary>
public static class MppgOutput
{
    /// <summary>
    /// The computed value of <paramref name="expression"/>, as the syntax writes it.
    /// </summary>
    public static string OfValue(IExpression expression)
        => expression switch
        {
            CurveExpression curve => curve.Value.ToMppgString(),
            RationalExpression rational => rational.Value.ToMppgString(),
            _ => throw new InvalidOperationException("The expression is neither a curve nor a rational.")
        };

    /// <summary>
    /// The expression itself, as the syntax writes it, or <paramref name="fallback"/> when it uses an
    /// operation the syntax has no notation for, which the renderer reports rather than write
    /// something that would not parse.
    /// </summary>
    public static string OfExpression(IExpression expression, string fallback)
    {
        try
        {
            return expression.ToMppgString();
        }
        catch (MppgFormattingException)
        {
            return fallback;
        }
    }
}
