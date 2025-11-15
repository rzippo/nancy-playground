using Spectre.Console;
using Unipi.MppgParser;
using Unipi.Nancy.Expressions;

namespace NancyMppg;

public class AnsiConsoleStatementFormatter : IStatementFormatter
{
    public IPlotFormatter? PlotFormatter { get; set; }
    
    public void FormatStatementPreamble(Statement statement)
    {
        switch (statement)
        {
            case Comment comment:
            {
                // do nothing
                break;
            }

            case EmptyStatement es:
            {
                // do nothing
                break;
            }

            default:
            {
                AnsiConsole.MarkupLineInterpolated($"> {statement.Text}");
                break;
            }
        }
    }

    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        switch (statement)
        {
            case ExpressionCommand expression:
            {
                var expressionOutput = (ExpressionOutput) output;
                if (expressionOutput.Expression.IsComputed)
                {
                    var expressionValue = expressionOutput.Expression switch
                    {
                        CurveExpression ce => ce.Value.ToCodeString(),
                        RationalExpression re => re.Value.ToString(),
                        _ => throw new InvalidOperationException()
                    };
                    AnsiConsole.MarkupLineInterpolated($"[blue][[{expressionOutput.Time}]][/] [magenta]{expressionValue}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLineInterpolated($"[blue][[{expressionOutput.Time}]][/] [magenta]{expressionOutput.OutputText}[/]");
                }
                break;
            }
            
            case Assignment assignment:
            {
                var assignmentOutput = (AssignmentOutput) output;
                if (assignmentOutput.Expression.IsComputed)
                {
                    var expressionValue = assignmentOutput.Expression switch
                    {
                        CurveExpression ce => ce.Value.ToCodeString(),
                        RationalExpression re => re.Value.ToString(),
                        _ => throw new InvalidOperationException()
                    };
                    AnsiConsole.MarkupLineInterpolated($"[blue][[{assignmentOutput.Time}]][/] {assignmentOutput.AssignedVariable} = [magenta]{expressionValue}[/]");

                }
                else
                {
                    AnsiConsole.MarkupLineInterpolated($"[blue][[{assignmentOutput.Time}]][/] {assignmentOutput.AssignedVariable} = [magenta]{assignmentOutput.Expression.ToUnicodeString()}[/]");
                }
                break;
            }

            case Comment comment:
            {
                AnsiConsole.MarkupLineInterpolated($"[green]{comment.Text}[/]");
                break;
            }

            case EmptyStatement es:
            {
                // do nothing
                break;
            }

            case PlotCommand plot:
            {
                if(PlotFormatter is not null)
                    PlotFormatter.FormatPlot((PlotOutput) output);
                else
                    AnsiConsole.MarkupLineInterpolated($"[yellow]Plots not supported.[/]");
                break;
            }

            default:
            {
                AnsiConsole.MarkupLineInterpolated($"{output.OutputText}");
                break;
            }
        }
    }

    public void FormatError(Statement statement, ErrorOutput error)
    {
        switch(error.Exception)
        {
            case SyntaxErrorException:
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Syntax error[/]: {error.Exception.Message}");
                break;
            }

            default:
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Execution error[/]: {error.Exception.Message}");
                break;
            }
        }

    }

    public void FormatEndOfProgram()
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]End of Program.[/]");
    }
}