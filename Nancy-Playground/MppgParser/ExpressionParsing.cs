using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// Parses an expression on its own, outside any statement.
/// </summary>
public static class ExpressionParsing
{
    /// <summary>
    /// Parses one expression, on its own, into the expression it builds.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    /// <param name="state">The variables of the session, whose types seed the parser.</param>
    /// <param name="syntaxVersion">The version to parse with, or the default for the latest.</param>
    /// <returns>A <see cref="CurveExpression"/> where the expression resolves to a function, and a <see cref="RationalExpression"/> where it resolves to a number.</returns>
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
