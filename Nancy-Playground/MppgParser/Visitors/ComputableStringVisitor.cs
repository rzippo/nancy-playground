using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Builds the string a plot option was given, which is literal text and the expressions written into it.
/// </summary>
public class ComputableStringVisitor : MppgBaseVisitor<ComputableString>
{
    /// <summary>
    /// Builds a string, i.e. the pieces it is concatenated from.
    /// </summary>
    public override ComputableString VisitString(Unipi.MppgParser.Grammar.MppgParser.StringContext context)
    {
        var cs = new ComputableString();
        for (int i = 0; i < context.ChildCount; i++)
        {
            var child = context.GetChild(i);
            var ics = Visit(child);
            if(ics != null)
                cs.Concat(ics);
        }

        return cs;
    }

    /// <summary>
    /// Appends a piece of literal text.
    /// </summary>
    public override ComputableString VisitStringLiteral(Unipi.MppgParser.Grammar.MppgParser.StringLiteralContext context)
    {
        var cs = new ComputableString();
        var str = context.GetText().TrimQuotes();
        cs.Append(str);
        return cs;
    }

    /// <summary>
    /// Appends a variable, to be evaluated when the string is computed.
    /// </summary>
    public override ComputableString VisitStringVariable(Unipi.MppgParser.Grammar.MppgParser.StringVariableContext context)
    {
        var cs = new ComputableString();
        var name = context.GetText();
        var expression = new Expression(name);
        cs.Append(expression);
        return cs;
    }

    /// <summary>
    /// Appends a number literal.
    /// </summary>
    public override ComputableString VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        var cs = new ComputableString();
        var visitor = new NumberLiteralVisitor();
        var number = visitor.Visit(context);
        cs.Append(number.ToString());
        return cs;
    }
}