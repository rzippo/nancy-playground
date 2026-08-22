using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// One expression of a statement, from the parse tree it was written as to the value it computes.
/// </summary>
public class Expression
{
    private enum ExpressionSourceType
    {
        ExpressionContext,
        NancyExpression,
        VariableName
    }

    private ExpressionSourceType SourceType { get; init; }

    /// <summary>
    /// The kind the expression resolves to, undetermined until it is parsed.
    /// </summary>
    public ExpressionType ExpressionType =>
        NancyExpression switch
        {
            CurveExpression => ExpressionType.Function,
            RationalExpression => ExpressionType.Number,
            _ => ExpressionType.Undetermined
        };

    /// <summary>
    /// The expression as Nancy builds it, once the parse tree has been visited.
    /// </summary>
    public IExpression? NancyExpression { get; internal set; }

    /// <summary>
    /// The parse tree, where the expression came from a script.
    /// </summary>
    public Unipi.MppgParser.Grammar.MppgParser.ExpressionContext? ExpressionContext { get; private set; }

    /// <summary>
    /// The name, where the expression is a reference to a variable.
    /// </summary>
    public string? VariableName { get; private set; }

    /// <summary>
    /// An expression already built with Nancy.
    /// </summary>
    public Expression(IExpression expression)
    {
        NancyExpression = expression;
        SourceType = ExpressionSourceType.NancyExpression;
    }

    /// <summary>
    /// An expression still to be visited, held as the tree it was parsed into.
    /// </summary>
    public Expression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context)
    {
        ExpressionContext = context;
        SourceType = ExpressionSourceType.ExpressionContext;
    }

    /// <summary>
    /// An expression that is a reference to the variable of the given name.
    /// </summary>
    public Expression(string variableName)
    {
        VariableName = variableName;
        SourceType = ExpressionSourceType.VariableName;
    }

    /// <summary>
    /// The expression <paramref name="context"/> builds, wrapped to be used as a statement operand.
    /// </summary>
    public static Expression FromTree(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var expression = ParseTree(context, state);
        return new Expression(expression);
    }

    /// <summary>
    /// Visits <paramref name="context"/> and returns the expression it builds.
    /// </summary>
    public static IExpression ParseTree(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var visitor = new ExpressionVisitor(state);
        var expression = visitor.Visit(context);
        return expression;
    }

    /// <summary>
    /// Resolves the expression against <paramref name="state"/>, which a tree or a variable name needs before it can be computed.
    /// </summary>
    public void ParseTree(State state)
    {
        switch (SourceType)
        {
            case ExpressionSourceType.ExpressionContext:
            {
                if(ExpressionContext == null)
                    throw new InvalidOperationException("Invalid state: no expression context");
                var expression = Expression.ParseTree(ExpressionContext, state);
                NancyExpression = expression;
                break;
            }
            case ExpressionSourceType.NancyExpression:
            {
                // do nothing
                break;
            }
            case ExpressionSourceType.VariableName:
            {
                if(string.IsNullOrWhiteSpace(VariableName))
                    throw new InvalidOperationException("Invalid state: no variable name");
                var expression = state.GetVariable(VariableName);
                NancyExpression = expression;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Computes the value, as a curve or as a number depending on which kind it resolves to.
    /// </summary>
    public (Curve? function, Rational? number) Compute()
    {
        if (NancyExpression is CurveExpression ce)
            return (ce.Compute(), null);
        else if (NancyExpression is RationalExpression re)
            return (null, re.Compute());
        else
            throw new InvalidOperationException("No expression was parsed!");
    }
}
