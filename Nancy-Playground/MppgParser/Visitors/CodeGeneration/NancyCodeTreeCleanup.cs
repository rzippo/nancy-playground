using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

/// <summary>
/// Syntax-aware cleanup for the code the code-tree visitors emit: they build conservatively,
/// wrapping a sub-expression in parentheses wherever its position might need them, without
/// checking whether it actually does once the whole tree is in hand. This strips the parentheses
/// that turn out unneeded, operating on the tree directly rather than by reparsing rendered text.
/// </summary>
internal static class NancyCodeTreeCleanup
{
    public static CompilationUnitSyntax RemoveRedundantParentheses(CompilationUnitSyntax compilationUnit) =>
        (CompilationUnitSyntax)new RedundantParenthesesRewriter().Visit(compilationUnit)!;

    private sealed class RedundantParenthesesRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            // Recurse first: a child paren can collapse into something the outer position no
            // longer needs wrapped (e.g. two nested defensive wraps collapse to one, then that
            // one turns out to sit directly against a cast, which is itself safe to unwrap here).
            // Deciding removability from the pre-recursion child would miss that cascade.
            var visited = (ParenthesizedExpressionSyntax)base.VisitParenthesizedExpression(node)!;
            return CanRemove(node.Parent, visited.Expression)
                ? visited.Expression.WithTriviaFrom(visited)
                : visited;
        }

        /// <summary>
        /// A parenthesized expression is safe to unwrap in two kinds of position: one where the
        /// position itself already delimits the expression, so nothing inside it could ever need
        /// disambiguating (an argument, an initializer value, or another pair of parentheses); or
        /// one where the parenthesized content binds at least as tightly as the position requires.
        /// A member access or a cast operand applies to the primary/postfix expression immediately
        /// following it, so only genuinely primary content is safe there — a cast itself is looser
        /// than postfix (unwrapping it would attach the surrounding member access, or the outer
        /// cast, to the cast's own operand instead of to its result). A binary or prefix-unary
        /// operand has no such trap, since a cast already binds tighter than either. Each parent
        /// kind has exactly one expression slot a parenthesized child could occupy (a binary
        /// expression's operator token isn't one), so which slot it is doesn't need checking.
        /// </summary>
        private static bool CanRemove(SyntaxNode? parent, ExpressionSyntax content) =>
            parent switch
            {
                ArgumentSyntax => true,
                EqualsValueClauseSyntax => true,
                ParenthesizedExpressionSyntax => true,
                MemberAccessExpressionSyntax => IsPrimaryLike(content),
                CastExpressionSyntax => IsPrimaryLike(content),
                BinaryExpressionSyntax => IsPrimaryOrCastLike(content),
                PrefixUnaryExpressionSyntax => IsPrimaryOrCastLike(content),
                _ => false
            };

        private static bool IsPrimaryLike(ExpressionSyntax expression) =>
            expression switch
            {
                IdentifierNameSyntax => true,
                LiteralExpressionSyntax => true,
                InvocationExpressionSyntax => true,
                MemberAccessExpressionSyntax => true,
                ObjectCreationExpressionSyntax => true,
                ImplicitObjectCreationExpressionSyntax => true,
                ElementAccessExpressionSyntax => true,
                _ => false
            };

        private static bool IsPrimaryOrCastLike(ExpressionSyntax expression) =>
            IsPrimaryLike(expression) || expression is CastExpressionSyntax;
    }
}
