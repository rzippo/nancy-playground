namespace Unipi.MppgParser;
using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser;

public class Assignment : Statement
{
    public string VariableName { get; set; }
    public Expression Expression { get; set; }

    public Assignment(string variableName, Expression expression)
    {
        VariableName = variableName;
        Expression = expression;
    }
    
    public override string Execute(State state)
        => Execute(state, true, false);

    public string Execute(
        State state, 
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
                    state.StoreVariable(VariableName, ce, overwrite, changeType);
                    break;
                case RationalExpression re:
                    state.StoreVariable(VariableName, re, overwrite, changeType);
                    break;
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
}