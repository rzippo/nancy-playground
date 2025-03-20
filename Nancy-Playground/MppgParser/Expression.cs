using Antlr4.Runtime;
using Unipi.MppgParser.Visitors;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.MppgParser;

public class Expression
{
    public ExpressionType ExpressionType =>
        FunctionExpression is not null ? ExpressionType.Function : 
        NumberExpression is not null ? ExpressionType.Number :
        ExpressionType.Undetermined;
    
    public CurveExpression? FunctionExpression { get; private set; }
    public RationalExpression? NumberExpression { get; private set; }

    public Grammar.MppgParser.ExpressionContext ExpressionContext { get; private set; }
    
    public Expression(CurveExpression? ce, RationalExpression? re)
    {
        FunctionExpression = ce;
        NumberExpression = re;
    }

    public Expression(Grammar.MppgParser.ExpressionContext context)
    {
        ExpressionContext = context;
    }

    public static Expression FromTree(Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var (ce, re) = ParseTree(context, state);
        return new Expression(ce, re);
    }
    
    public static (CurveExpression? Function, RationalExpression? Number) ParseTree(Grammar.MppgParser.ExpressionContext context, State? state)
    {
        var visitor = new ExpressionVisitor(state);
        var (ce, re) = visitor.Visit(context);
        return (ce, re);
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
    public static (CurveExpression? Function, RationalExpression? Number) ParseFromString(string expression, State? state)
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
        var (ce, re) = Expression.ParseTree(ExpressionContext, state);
        FunctionExpression = ce;
        NumberExpression = re;
    }

    public (Curve? function, Rational? number) Compute()
    {
        if (FunctionExpression is not null)
            return (FunctionExpression.Compute(), null);
        else if (NumberExpression is not null)
            return (null, NumberExpression.Compute());
        else
            throw new InvalidOperationException("No expression was parsed!");
    }
}