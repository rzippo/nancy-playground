using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Unipi.MppgParser.Grammar;
using Unipi.MppgParser.Utility;

namespace Unipi.MppgParser.Visitors;

public class StatementVisitor : MppgBaseVisitor<Statement>
{
    public override Statement VisitStatementLine([NotNull] Grammar.MppgParser.StatementLineContext context)
    {
        var statementContext = context.GetChild<Grammar.MppgParser.StatementContext>(0);
        var inlineCommentContext = context.GetChild<Grammar.MppgParser.InlineCommentContext>(0);

        var statement = statementContext.Accept(this);
        if (inlineCommentContext != null)
        {
            var inlineComment = inlineCommentContext.GetJoinedText();
            if (statement is Comment c)
                return c with
                {
                    Text = $"{c.Text} {inlineComment}"
                };
            else
                return statement with
                {
                    InlineComment = inlineComment
                };
        }
        else
            return statement;
    }

    public override Statement VisitComment(Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        return new Comment { Text = text };
    }

    public override Statement VisitEmpty([NotNull] Grammar.MppgParser.EmptyContext context)
    {
        return new EmptyStatement();
    }

    public override Statement VisitPlotCommand(Grammar.MppgParser.PlotCommandContext context)
    {
        var text = context.GetJoinedText();
        var args = context.GetRuleContexts<Grammar.MppgParser.PlotArgContext>();

        var functionNameContexts = args
            .Select(arg => arg.GetChild<Grammar.MppgParser.FunctionNameContext>(0))
            .Where(ctx => ctx != null);
        var plotOptionContexts = args
            .Select(arg => arg.GetChild<Grammar.MppgParser.PlotOptionContext>(0))
            .Where(ctx => ctx != null);
        
        var variableNames = functionNameContexts
            .Select(ctx => ctx.GetText())
            .Select(name => new Expression(name))
            .ToList();

        var settings = new PlotSettings();
        foreach (var plotArgContext in plotOptionContexts)
        {
            var argName = plotArgContext.GetChild(0).GetText();
            var argString = plotArgContext.GetChild(2).GetText()
                .TrimQuotes();

            switch (argName)
            {
                case "main":
                {
                    // todo
                    break;
                }
                
                case "xlim":
                {
                    // todo
                    break;
                }
                
                case "ylim":
                {
                    // todo
                    break;
                }
                
                case "xlab":
                {
                    // todo
                    break;
                }
                
                case "ylab":
                {
                    // todo
                    break;
                }
                
                case "out":
                {
                    settings = settings with
                    {
                        OutPath = argString
                    };
                    break;
                }
                
                case "grid":
                {
                    settings = settings with
                    {
                        ShowGrid = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                case "bg":
                {
                    settings = settings with
                    {
                        ShowBackground = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                case "browser":
                {
                    settings = settings with
                    {
                        ShowInBrowser = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                default:
                    // do nothing
                    break;
            }
        }

        return new PlotCommand
        {
            FunctionsToPlot = variableNames,
            Text = text,
            Settings = settings
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