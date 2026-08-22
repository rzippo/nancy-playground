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
        var version = syntaxVersion == default ? SyntaxVersion.Latest : syntaxVersion;
        var parse = MppgParsing.Create(expression, ErrorRecovery.FirstError, version, state);

        // the entry rule is anchored at EOF, so input left over after the expression is reported
        var context = parse.ParseOrThrow(static parser => parser.expressionEntry()).expression();

        var visitor = new ExpressionVisitor(state);

        return context.Accept(visitor);
    }
}
