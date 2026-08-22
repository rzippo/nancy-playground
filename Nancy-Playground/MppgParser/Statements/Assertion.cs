using System.Diagnostics;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// Assertions, composed of two expressions and a comparison operator.
/// Used to test out values of variables.
/// </summary>
public record class Assertion : Statement
{
    /// <summary>
    /// The comparison an assertion makes between its two sides.
    /// </summary>
    public enum AssertionOperator
    {
        /// <summary>
        /// The two sides are equal.
        /// </summary>
        Equal,
        /// <summary>
        /// The two sides differ.
        /// </summary>
        NotEqual,
        /// <summary>
        /// The left side is smaller.
        /// </summary>
        Less,
        /// <summary>
        /// The left side is smaller, or the two are equal.
        /// </summary>
        LessOrEqual,
        /// <summary>
        /// The left side is greater.
        /// </summary>
        Greater,
        /// <summary>
        /// The left side is greater, or the two are equal.
        /// </summary>
        GreaterOrEqual
    }

    /// <summary>
    /// The side written before the operator.
    /// </summary>
    public Expression LeftExpression { get; set; }

    /// <summary>
    /// The side written after the operator.
    /// </summary>
    public Expression RightExpression { get; set; }

    /// <summary>
    /// The comparison the two sides are held to.
    /// </summary>
    public AssertionOperator Operator { get; set; }
    /// <summary>
    /// An assertion comparing two expressions.
    /// </summary>
    public Assertion(Expression leftExpression, Expression rightExpression, AssertionOperator @operator)
    {
        LeftExpression = leftExpression;
        RightExpression = rightExpression;
        Operator = @operator;
    }

    /// <summary>
    /// Evaluates both sides and reports whether the comparison holds.
    /// </summary>
    public override string Execute(State state)
    {
        LeftExpression.ParseTree(state);
        var (lc, lr) = LeftExpression.Compute();

        RightExpression.ParseTree(state);
        var (rc, rr) = RightExpression.Compute();

        bool? result = null;
        if (lc is not null && rc is not null)
            result = CompareCurves(lc, rc);
        else if (lc is not null && rr is not null)
            result = CompareCurves(lc, new PureConstantCurve(rr ?? 0));
        else if (lr is not null && rc is not null)
            result = CompareCurves(new PureConstantCurve(lr ?? 0), rc);
        else if (lr is not null && rr is not null)
            result = CompareRationals(lr ?? 0, rr ?? 0);

        if (result is not null)
            return (result ?? false) ? "true" : "false";
        else
            return "Invalid?";
    }

    private bool CompareCurves(Curve left, Curve right)
    {
        var result = Operator switch
        {
            AssertionOperator.Equal => Curve.Equivalent(left, right),
            AssertionOperator.NotEqual => !Curve.Equivalent(left, right),
            AssertionOperator.Less => left <= right && !Curve.Equivalent(left, right),
            AssertionOperator.LessOrEqual => left <= right,
            AssertionOperator.Greater => left >= right && !Curve.Equivalent(left, right),
            AssertionOperator.GreaterOrEqual => left >= right,
            _ => throw new NotImplementedException($"Assertion {Operator.ToString()} not implemented")
        };
        return result;
    }

    private bool CompareRationals(Rational left, Rational right)
    {
        var result = Operator switch
        {
            AssertionOperator.Equal => left == right,
            AssertionOperator.NotEqual => left != right,
            AssertionOperator.Less => left < right,
            AssertionOperator.LessOrEqual => left <= right,
            AssertionOperator.Greater => left > right,
            AssertionOperator.GreaterOrEqual => left >= right,
            _ => throw new NotImplementedException($"Assertion {Operator.ToString()} not implemented")
        };
        return result;
    }

    /// <summary>
    /// Evaluates both sides and reports whether the comparison holds, for a formatter to render.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        var sw = Stopwatch.StartNew();
        LeftExpression.ParseTree(state);
        var (lc, lr) = LeftExpression.Compute();

        RightExpression.ParseTree(state);
        var (rc, rr) = RightExpression.Compute();

        bool? result = null;
        if (lc is not null && rc is not null)
            result = CompareCurves(lc, rc);
        else if (lc is not null && rr is not null)
            result = CompareCurves(lc, new PureConstantCurve(rr ?? 0));
        else if (lr is not null && rc is not null)
            result = CompareCurves(new PureConstantCurve(lr ?? 0), rc);
        else if (lr is not null && rr is not null)
            result = CompareRationals(lr ?? 0, rr ?? 0);
        sw.Stop();

        var output = (result is not null) 
            ? ((result ?? false) ? "true" : "false")
            : "Invalid?";

        return new AssertionOutput
        {
            StatementText = Text,
            OutputText = output,
            Result = result ?? false,
            LeftExpression = LeftExpression.NancyExpression!,
            RightExpression = RightExpression.NancyExpression!,
            Time = sw.Elapsed,
        };
    }
}