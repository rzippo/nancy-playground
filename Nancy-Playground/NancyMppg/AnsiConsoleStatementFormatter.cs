using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.MppgParser;
using Unipi.Nancy.Expressions;

namespace NancyMppg;

public class AnsiConsoleStatementFormatter : IStatementFormatter
{
    public void FormatStatementPreamble(Statement statement)
    {
        switch (statement)
        {
            case Comment comment:
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
                AnsiConsole.MarkupLineInterpolated($"[blue][[{expressionOutput.Time}]][/] [magenta]{expressionOutput.OutputText}[/]");
                break;
            }
            
            case Assignment assignment:
            {
                var assignmentOutput = (AssignmentOutput) output;
                AnsiConsole.MarkupLineInterpolated($"[blue][[{assignmentOutput.Time}]][/] {assignmentOutput.AssignedVariable} = [magenta]{assignmentOutput.Expression.ToUnicodeString()}[/]");
                break;
            }

            case Comment comment:
            {
                AnsiConsole.MarkupLineInterpolated($"[green]{comment.Text}[/]");
                break;
            }

            case PlotCommand plot:
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]Not supported.[/]");
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
        AnsiConsole.MarkupLineInterpolated($"[red]ERROR[/] {error.Exception.Message}");
    }

    public void FormatEndOfProgram()
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]End of Program.[/]");
    }
}