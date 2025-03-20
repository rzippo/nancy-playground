using Antlr4.Runtime;
using Unipi.MppgParser.Visitors;
using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser;

public static class ExpressionParsing
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns>
    /// This method returns *either* a <see cref="CurveExpression"/>, if the expression resolves to a function,
    /// *or* a <see cref="RationalExpression"/> if the expression resolves to a number.
    /// The returned tuple will have null for the other type.  
    /// </returns>
    public static (CurveExpression? Function, RationalExpression? Number) Parse(string expression, State? state)
    {
        var inputStream = CharStreams.fromString(expression);
        var lexer = new Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Grammar.MppgParser(commonTokenStream);

        var context = parser.expression();
        var visitor = new ExpressionVisitor(state);
     
        return context.Accept(visitor);
    }
}