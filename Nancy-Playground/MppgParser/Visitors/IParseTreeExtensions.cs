using Antlr4.Runtime.Tree;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Reads the text back out of a parse tree.
/// </summary>
public static class IParseTreeExtensions
{
    /// <summary>
    /// The text of each child of <paramref name="tree"/>, in order.
    /// </summary>
    public static List<string> GetChildText(this IParseTree tree)
    {
        if (tree.ChildCount == 0)
        {
            return [ tree.GetText() ];
        }
        else
        {
            var result = new List<string>();
            for (int i = 0; i < tree.ChildCount; i++)
            {
                var child = tree.GetChild(i);
                result.AddRange(child.GetChildText());
            }
            return result;
        }
    }

    /// <summary>
    /// The text of the children of <paramref name="tree"/>, joined by <paramref name="separator"/>.
    /// </summary>
    public static string GetJoinedText(this IParseTree tree, string separator = " ")
    {
        return string.Join(separator, GetChildText(tree));
    }

    /// <summary>
    /// True if any of the given statement lines is a <c>plotTikz</c> command,
    /// i.e. the program needs Nancy.Plots.Tikz.
    /// </summary>
    public static bool UsesTikzPlots(
        this IEnumerable<Unipi.MppgParser.Grammar.MppgParser.StatementLineContext> statementLineContexts
    )
    {
        return statementLineContexts
            .Select(line => line.GetChild<Unipi.MppgParser.Grammar.MppgParser.StatementContext>(0))
            .Where(statement => statement is not null)
            .Any(statement => statement.GetChild<Unipi.MppgParser.Grammar.MppgParser.PlotTikzCommandContext>(0) is not null);
    }
}