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
            if (Expression.ExpressionType == ExpressionType.Function)
                state.StoreVariable(VariableName, Expression.FunctionExpression!, overwrite, changeType);
            else
                state.StoreVariable(VariableName, Expression.NumberExpression!, overwrite, changeType);

            return VariableName;
        }
        catch (Exception e)
        {
            return e.Message;   
        }
    }
}