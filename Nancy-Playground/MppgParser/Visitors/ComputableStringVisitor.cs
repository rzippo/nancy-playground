using Antlr4.Runtime.Tree;
using Unipi.MppgParser.Grammar;
using Unipi.MppgParser.Utility;

namespace Unipi.MppgParser.Visitors;

public class ComputableStringVisitor : MppgBaseVisitor<ComputableString>
{
    public override ComputableString VisitString(Grammar.MppgParser.StringContext context)
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

    public override ComputableString VisitStringLiteral(Grammar.MppgParser.StringLiteralContext context)
    {
        var cs = new ComputableString();
        var str = context.GetText().TrimQuotes();
        cs.Append(str);
        return cs;
    }

    public override ComputableString VisitStringVariable(Grammar.MppgParser.StringVariableContext context)
    {
        var cs = new ComputableString();
        var name = context.GetText();
        var expression = new Expression(name);
        cs.Append(expression);
        return cs;
    }

    public override ComputableString VisitNumberLiteral(Grammar.MppgParser.NumberLiteralContext context)
    {
        var cs = new ComputableString();
        var visitor = new NumberLiteralVisitor();
        var number = visitor.Visit(context);
        cs.Append(number.ToString());
        return cs;
    }
}