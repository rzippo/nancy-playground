using Antlr4.Runtime;
using Unipi.MppgParser.Grammar;

namespace Unipi.MppgParser.Visitors;

public class StatementVisitor : MppgBaseVisitor<Statement>
{
    public override Statement VisitComment(Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        return new Comment { Text = text };
    }

    public override Statement VisitPlotCommand(Grammar.MppgParser.PlotCommandContext context)
    {
        var text = context.GetJoinedText();
        var functionsToPlotContext = context.GetChild<Grammar.MppgParser.FunctionsToPlotContext>(0);
        var functionNameContexts = functionsToPlotContext.GetRuleContexts<Grammar.MppgParser.FunctionNameContext>();
        
        var variableNames = functionNameContexts
            .Select(ctx => ctx.GetText())
            .Select(name => new Expression(name))
            .ToList();

        var plotArgsContext = context.GetChild<Grammar.MppgParser.PlotArgsContext>(0);
        var plotSettingsVisitor = new PlotSettingsVisitor();
        var plotSettings = plotSettingsVisitor.Visit(plotArgsContext);

        return new PlotCommand
        {
            FunctionsToPlot = variableNames,
            Text = text,
            Settings = plotSettings
        };
    }

    public override Statement VisitExpression(Grammar.MppgParser.ExpressionContext context)
    {
        var expression = new Expression(context);
        var text = context.GetJoinedText();
        return new ExpressionCommand(expression) { Text = text };
    }

    public override Statement VisitAssignment(Grammar.MppgParser.AssignmentContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var text = context.GetJoinedText();
        var name = context.GetChild(0).GetText();
        var expressionContext = (Grammar.MppgParser.ExpressionContext) context.GetChild(2);
        var expression = new Expression(expressionContext);
          
        return new Assignment(name, expression) { Text = text };
    }

    public override Statement VisitPrintExpressionCommand(Grammar.MppgParser.PrintExpressionCommandContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var name = context.GetChild(2).GetText();
        var text = context.GetJoinedText();

        return new PrintExpressionCommand(name) { Text = text };
    }
}