using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

internal static class ExpressionContextTypeExtensions
{
    public static ExpressionType GetExpressionType(
        this Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context)
    {
        if (context.functionExpression() is not null)
            return ExpressionType.Function;

        if (context.numberExpression() is not null)
            return ExpressionType.Number;

        throw new InvalidOperationException("Expression was not parsed as either a function or a number.");
    }
}
