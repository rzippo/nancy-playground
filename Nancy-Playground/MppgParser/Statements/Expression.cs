using Antlr4.Runtime;
using Unipi.MppgParser.Visitors;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.MppgParser;

public class Expression
{
    public ExpressionType ExpressionType =>
        NancyExpression switch
        {
            CurveExpression => ExpressionType.Function,
            RationalExpression => ExpressionType.Number,
            _ => ExpressionType.Undetermined
        };
    
    public IExpression? NancyExpression { get; private set; }
    
    public Grammar.MppgParser.ExpressionContext ExpressionContext { get; private set; }
    
    public Expression(IExpression expression)
    {
        NancyExpression = expression;
    }

    public Expression(Grammar.MppgParser.ExpressionContext context)
    {
        ExpressionContext = context;
    }

    public static Expression FromTree(Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var expression = ParseTree(context, state);
        return new Expression(expression);
    }
    
    public static IExpression ParseTree(Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var visitor = new ExpressionVisitor(state);
        var epression = visitor.Visit(context);
        return epression;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns>
    /// This method returns *either* a <see cref="CurveExpression"/>, if the expression resolves to a function,
    /// *or* a <see cref="RationalExpression"/> if the expression resolves to a number.
    /// The returned tuple will have null for the other type.  
    /// </returns>
    public static IExpression ParseFromString(string expression, State? state)
    {
        var inputStream = CharStreams.fromString(expression);
        var lexer = new Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Grammar.MppgParser(commonTokenStream);

        var context = parser.expression();
        var visitor = new ExpressionVisitor(state);
     
        return context.Accept(visitor);
    }

    public void ParseTree(State state)
    {
        var expression = Expression.ParseTree(ExpressionContext, state);
        NancyExpression = expression;
    }

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