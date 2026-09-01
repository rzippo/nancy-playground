using System.Diagnostics;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// Assertions of one property of one expression, e.g. <c>assert(f is subadditive)</c>.
/// </summary>
public record class PropertyAssertion : Statement
{
    /// <summary>
    /// The expression the property is tested against.
    /// </summary>
    public Expression Operand { get; set; }

    /// <summary>
    /// The property tested.
    /// </summary>
    public AssertProperties.Definition Property { get; set; }

    /// <summary>
    /// True for <c>is not</c>, which negates the property.
    /// </summary>
    public bool Negated { get; set; }

    /// <summary>
    /// An assertion of one property of one expression.
    /// </summary>
    public PropertyAssertion(Expression operand, AssertProperties.Definition property, bool negated)
    {
        Operand = operand;
        Property = property;
        Negated = negated;
    }

    /// <summary>
    /// Reads <see cref="Property"/> off the materialized value, negating it if <see cref="Negated"/>.
    /// </summary>
    private bool Evaluate(Curve? curve, Rational? rational)
    {
        var raw = curve is not null
            ? (bool)typeof(Curve).GetProperty(Property.NancyMember)!.GetValue(curve)!
            : (bool)typeof(Rational).GetProperty(Property.NancyMember)!.GetValue(rational!)!;
        return Negated ? !raw : raw;
    }

    /// <summary>
    /// Evaluates the operand and reports whether the property holds.
    /// </summary>
    public override string Execute(State state)
    {
        Operand.ParseTree(state);
        var (curve, rational) = Operand.Compute();
        return Evaluate(curve, rational) ? "true" : "false";
    }

    /// <summary>
    /// Evaluates the operand and reports whether the property holds, for a formatter to render.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        var sw = Stopwatch.StartNew();
        Operand.ParseTree(state);
        var (curve, rational) = Operand.Compute();
        var result = Evaluate(curve, rational);
        sw.Stop();

        return new PropertyAssertionOutput
        {
            StatementText = Text,
            OutputText = result ? "true" : "false",
            Result = result,
            Operand = Operand.NancyExpression!,
            Time = sw.Elapsed,
            Warnings = Operand.ExecutionWarnings,
        };
    }
}
