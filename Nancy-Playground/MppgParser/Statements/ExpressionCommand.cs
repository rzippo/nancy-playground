using System.Diagnostics;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// Statements without assignement.
/// Most commonly used to print-out the value of a variable.
/// </summary>
public record class ExpressionCommand : Statement
{
    public Expression Expression { get; set; }

    public ExpressionCommand(Expression expression)
    {
        Expression = expression;
    }

    public override string Execute(State state)
        => ExecuteToFormattable(state).OutputText;

    public override StatementOutput ExecuteToFormattable(State state)
    {
        var sw = Stopwatch.StartNew();
        Expression.ParseTree(state);
        switch (Expression.NancyExpression)
        {
            case CurveExpression ce:
            {
                ce.Compute();
                break;
            }
            case RationalExpression re:
            {
                re.Compute();
                break;
            }
            default:
                throw new Exception($"Expression could not be parsed");
        }
        sw.Stop();

        // the default notation is the syntax itself; a formatter that wants another one renders it
        // from the expression this output carries
        var output = MppgOutput.OfValue(Expression.NancyExpression!);

        return new ExpressionOutput()
        {
            StatementText = Text,
            OutputText = output,
            Expression = Expression.NancyExpression,
            Time = sw.Elapsed,
        };
    }
}