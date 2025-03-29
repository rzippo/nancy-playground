namespace Unipi.MppgParser;

public class ExpressionCommand : Statement
{
    public Expression Expression { get; set; }

    public ExpressionCommand(Expression expression)
    {
        Expression = expression;
    }
    
    public override string Execute(State state)
    {
        Expression.ParseTree(state);
        var (c, r) = Expression.Compute();
        
        if (c is not null)
            return c.ToCodeString();
        if (r is not null)
            return r.ToString()!;
        else
            return "undefined";
    }
}