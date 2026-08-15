using Antlr4.Runtime.Misc;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public class StatementVisitor : MppgBaseVisitor<Statement?>
{
    private readonly SyntaxErrorInfo? _syntaxError;
    private readonly SyntaxVersion _syntaxVersion;

    public StatementVisitor(SyntaxErrorInfo? syntaxError = null, SyntaxVersion syntaxVersion = default)
    {
        _syntaxError = syntaxError;
        _syntaxVersion = syntaxVersion == default ? SyntaxVersion.Latest : syntaxVersion;
    }

    public override Statement? VisitStatementLine([NotNull] Unipi.MppgParser.Grammar.MppgParser.StatementLineContext context)
    {
        if (_syntaxError is not null)
            return CreateSyntaxErrorStatement(context);

        var statementContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.StatementContext>(0);
        var inlineCommentContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.InlineCommentContext>(0);

        Statement? statement;
        try
        {
            statement = statementContext?.Accept(this);
        }
        catch (Exception ex)
        {
            return CreateSyntaxErrorStatement(context, ex);
        }

        if (statement is null)
            return CreateSyntaxErrorStatement(context);

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

    public override Statement? VisitComment(Unipi.MppgParser.Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        return new Comment { Text = text };
    }

    public override Statement? VisitVersionDirective(Unipi.MppgParser.Grammar.MppgParser.VersionDirectiveContext context)
    {
        var text = context.GetJoinedText();
        SyntaxVersion.TryParseShebang(text, out var version);
        return new VersionDirectiveStatement(version) { Text = text };
    }

    public override Statement? VisitDirective(Unipi.MppgParser.Grammar.MppgParser.DirectiveContext context)
    {
        var text = context.GetJoinedText();
        return new DirectiveStatement { Text = text };
    }

    public override Statement? VisitEmpty(Unipi.MppgParser.Grammar.MppgParser.EmptyContext context)
    {
        return new EmptyStatement();
    }

    public override Statement? VisitPlotCommand(Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context)
    {
        var (variableNames, settings) = ParsePlotArgs(context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.PlotArgContext>(), PlotOutputKind.Image);

        return new PlotCommand
        {
            FunctionsToPlot = variableNames,
            Text = MppgReformatVisitor.Reformat(context),
            Settings = settings
        };
    }

    public override Statement? VisitPlotTikzCommand(Unipi.MppgParser.Grammar.MppgParser.PlotTikzCommandContext context)
    {
        var (variableNames, settings) = ParsePlotArgs(context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.PlotArgContext>(), PlotOutputKind.Tikz);

        return new PlotTikzCommand
        {
            FunctionsToPlot = variableNames,
            Text = MppgReformatVisitor.Reformat(context),
            Settings = settings
        };
    }

    /// <summary>
    /// Parses the arguments shared by <c>plot</c> and <c>plotTikz</c>, i.e. the functions to plot and the plot settings.
    /// </summary>
    /// <param name="args">The argument contexts of the plot command.</param>
    /// <param name="outputKind">The kind of output of the plot command, which determines the extension of the <c>out</c> option.</param>
    private static (List<Expression> FunctionsToPlot, PlotSettings Settings) ParsePlotArgs(
        Unipi.MppgParser.Grammar.MppgParser.PlotArgContext[] args,
        PlotOutputKind outputKind)
    {
        var functionNameContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0))
            .Where(ctx => ctx != null);
        var plotOptionContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext>(0))
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
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        Title = cs
                    };
                    break;
                }

                case "title":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        Title = cs
                    };
                    break;
                }

                case "xlim":
                {
                    var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
                    var numberVisitor = new NumberLiteralVisitor();
                    var leftLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext>(0);
                    var rightLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext>(1);
                    var leftLimit = numberVisitor.Visit(leftLimitContext);
                    var rightLimit = numberVisitor.Visit(rightLimitContext);
                    settings = settings with
                    {
                        XLimit = (leftLimit, rightLimit)
                    };
                    break;
                }

                case "ylim":
                {
                    var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
                    var numberVisitor = new NumberLiteralVisitor();
                    var leftLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext>(0);
                    var rightLimitContext = intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext>(1);
                    var leftLimit = numberVisitor.Visit(leftLimitContext);
                    var rightLimit = numberVisitor.Visit(rightLimitContext);
                    settings = settings with
                    {
                        YLimit = (leftLimit, rightLimit)
                    };
                    break;
                }

                case "xlab":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        XLabel = cs
                    };
                    break;
                }

                case "ylab":
                {
                    var stringContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringContext>(0);
                    var visitor = new ComputableStringVisitor();
                    var cs = visitor.Visit(stringContext);
                    settings = settings with
                    {
                        YLabel = cs
                    };
                    break;
                }

                case "out":
                {
                    settings = settings with
                    {
                        OutPath = PlotOutPath.Resolve(argString, outputKind)
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

                case "gui":
                {
                    settings = settings with
                    {
                        ShowInGui = argString switch
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

        return (variableNames, settings);
    }

    public override Statement? VisitExpression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context)
    {
        var expression = new Expression(context);
        var text = MppgReformatVisitor.Reformat(context);
        return new ExpressionCommand(expression) { Text = text };
    }

    public override Statement? VisitAssignment(Unipi.MppgParser.Grammar.MppgParser.AssignmentContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var name = context.GetChild(0).GetText();
        var expressionContext = (Unipi.MppgParser.Grammar.MppgParser.ExpressionContext) context.GetChild(2);
        var expression = new Expression(expressionContext);
        var text = $"{name} := {MppgReformatVisitor.Reformat(expressionContext)}";
          
        return new Assignment(name, expression) { Text = text };
    }

    public override Statement? VisitPrintExpressionCommand(Unipi.MppgParser.Grammar.MppgParser.PrintExpressionCommandContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");
        
        var name = context.GetChild(2).GetText();
        var text = MppgReformatVisitor.Reformat(context);

        return new PrintExpressionCommand(name) { Text = text };
    }

    public override Statement? VisitAssertion(Unipi.MppgParser.Grammar.MppgParser.AssertionContext context)
    {
        var leftExpressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        var rightExpressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(1);
        var operatorContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.AssertionOperatorContext>(0);
        
        var leftExpression = new Expression(leftExpressionContext);
        var rightExpression = new Expression(rightExpressionContext);
        var operatorText = operatorContext.GetJoinedText(); 
        var @operator = operatorText switch
        {
            "=" => Assertion.AssertionOperator.Equal,
            "!=" => Assertion.AssertionOperator.NotEqual,
            "<" => Assertion.AssertionOperator.Less,
            "<=" => Assertion.AssertionOperator.LessOrEqual,
            ">" => Assertion.AssertionOperator.Greater,
            ">=" => Assertion.AssertionOperator.GreaterOrEqual,
            _ => throw new ArgumentException($"Operator '{operatorText}' not recognized")
        };

        var text = MppgReformatVisitor.Reformat(context);

        return new Assertion(leftExpression, rightExpression, @operator){ Text = text };
    }

    private SyntaxErrorStatement CreateSyntaxErrorStatement(
        Unipi.MppgParser.Grammar.MppgParser.StatementLineContext context,
        Exception? innerException = null)
    {
        return new SyntaxErrorStatement
        {
            Text = context.GetJoinedText(),
            SyntaxError = _syntaxError,
            InnerException = innerException,
            Message = "Statement could not be parsed."
        };
    }
}
