using Antlr4.Runtime;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser;

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
    public static IExpression Parse(string expression, State? state, SyntaxVersion syntaxVersion = default)
    {
        var inputStream = CharStreams.fromString(expression);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        var version = syntaxVersion == default ? SyntaxVersion.Latest : syntaxVersion;
        lexer.SetSyntaxVersion(version.Major, version.Minor);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);
        parser.ErrorHandler = new BailErrorStrategy();
        parser.SeedVariableTypes(state);

        Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context;
        try
        {
            context = parser.expression();
        }
        catch (Exception ex)
        {
            throw ex.WithVersionedKeywordHint(commonTokenStream);
        }

        var visitor = new ExpressionVisitor(state);

        return context.Accept(visitor);
    }
}
