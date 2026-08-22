using System.Diagnostics;
using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// An assignment, which gives a name to the expression on its right.
/// </summary>
public record class Assignment : Statement
{
    /// <summary>
    /// The name being assigned to.
    /// </summary>
    public string VariableName { get; set; }
    /// <summary>
    /// The expression assigned to it.
    /// </summary>
    public Expression Expression { get; set; }

    /// <summary>
    /// An assignment of <paramref name="expression"/> to <paramref name="variableName"/>.
    /// </summary>
    public Assignment(string variableName, Expression expression)
    {
        VariableName = variableName;
        Expression = expression;
    }

    /// <summary>
    /// Stores the expression under its name, leaving it to be computed when required.
    /// </summary>
    public override string Execute(State state)
        => Execute(state, false, true, false);

    /// <summary>
    /// Stores the expression under its name.
    /// <paramref name="computeValue"/> computes it there and then, <paramref name="overwrite"/> allows an existing variable to be replaced, and <paramref name="changeType"/> one of the other kind.
    /// </summary>
    public string Execute(
        State state,
        bool computeValue,
        bool overwrite = true, 
        bool changeType = false
    )
    {
        try
        {
            Expression.ParseTree(state);
            switch (Expression.NancyExpression)
            {
                case CurveExpression ce:
                {
                    if(computeValue) 
                        ce.Compute();
                    state.StoreVariable(VariableName, ce, overwrite, changeType);
                    break;
                }
                case RationalExpression re:
                {
                    if(computeValue) 
                        re.Compute();
                    state.StoreVariable(VariableName, re, overwrite, changeType);
                    break;
                }
                default:
                    throw new Exception($"Expression could not be parsed");
            }

            return VariableName;
        }
        catch (Exception e)
        {
            return e.Message;   
        }
    }

    /// <summary>
    /// Stores the expression under its name, for a formatter to render.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
        => ExecuteToFormattable(state, false, true, false);

    /// <summary>
    /// Stores the expression under its name, for a formatter to render.
    /// <paramref name="immediateComputeValue"/> computes it there and then, <paramref name="overwrite"/> allows an existing variable to be replaced, and <paramref name="changeType"/> one of the other kind.
    /// </summary>
    public AssignmentOutput ExecuteToFormattable(
        State state,
        bool immediateComputeValue,
        bool overwrite = true, 
        bool changeType = false
    )
    {
        var sw = Stopwatch.StartNew();
        Expression.ParseTree(state);
        switch (Expression.NancyExpression)
        {
            case CurveExpression ce:
            {
                if(immediateComputeValue)
                    ce.Compute();
                state.StoreVariable(VariableName, ce, overwrite, changeType);
                break;
            }
            case RationalExpression re:
            {
                if(immediateComputeValue)
                    re.Compute();
                state.StoreVariable(VariableName, re, overwrite, changeType);
                break;
            }
            default:
                throw new Exception($"Expression could not be parsed");
        }
        sw.Stop();

        return new AssignmentOutput
        {
            StatementText = Text,
            OutputText = VariableName,
            AssignedVariable = VariableName,
            Expression = Expression.NancyExpression,
            Time = sw.Elapsed,
        };
    }
}