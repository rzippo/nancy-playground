using Spectre.Console;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.Cli.Utility;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Writes what a statement produced with the colours and the layout of the interactive session.
/// </summary>
public class AnsiConsoleStatementFormatter : IStatementFormatter
{
    /// <summary>
    /// What draws a plot, or null where the session draws none.
    /// </summary>
    public IPlotFormatter? PlotFormatter { get; init; }

    /// <summary>
    /// Used to render <c>plotTikz</c> commands.
    /// If null, TikZ plots are disabled.
    /// </summary>
    public TikzPlotFormatter? TikzPlotFormatter { get; init; }

    /// <summary>
    /// If true, the statement text is printed in gray, as confirmation to a prompt above (e.g., in interactive mode).
    /// If false, it is instead printed in $mainColor (e.g., in run mode). 
    /// </summary>
    public bool PrintInputAsConfirmation { get; init; } = false;

    /// <summary>
    /// If true, echoes user input in interactive mode.
    /// If false, input is only echoed on syntax errors.
    /// </summary>
    public bool EchoInput { get; init; } = false;

    /// <summary>
    /// True for verbose output, meant for debugging.
    /// </summary>
    public bool Verbose { get; init; } = false;

    /// <summary>
    /// If true, prints the time taken to execute each statement.
    /// </summary>
    public bool PrintTimePerStatement { get; init; } = true;

    /// <summary>
    /// The console used for output.
    /// Defaults to a stdout console.
    /// </summary>
    public IAnsiConsole Console { get; init; } = AnsiConsole.Console;

    /// <summary>
    /// Echoes the statement before it runs, where the settings ask for it.
    /// </summary>
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
                if(EchoInput)
                {
                    if(PrintInputAsConfirmation)
                    {
                        // use gray text, to not attract focus
                        if (statement.InlineComment.IsNullOrWhiteSpace())
                            Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/]");
                        else
                            Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/] [green]{statement.InlineComment}[/]");
                    }
                    else
                    {
                        // use $mainColor text
                        if (statement.InlineComment.IsNullOrWhiteSpace())
                            Console.MarkupLineInterpolated($"> {statement.Text}");
                        else
                            Console.MarkupLineInterpolated($"> {statement.Text} [green]{statement.InlineComment}[/]");
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Writes what the statement produced.
    /// </summary>
    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        foreach (var warning in statement.Warnings)
            Console.MarkupLineInterpolated($"[yellow]{warning}[/]");

        switch (statement)
        {
            case ExpressionCommand expression:
            {
                var expressionOutput = (ExpressionOutput) output;
                var formattedTime = FormatStatementTime(expressionOutput.Time);
                if (expressionOutput.Expression.IsComputed)
                {
                    var expressionValue = MppgOutput.OfValue(expressionOutput.Expression);
                    Console.MarkupLineInterpolated(formattedTime.Concat($"[magenta]{expressionValue}[/]"));
                }
                else
                {
                    Console.MarkupLineInterpolated(formattedTime.Concat($"[magenta]{expressionOutput.OutputText}[/]"));
                }
                break;
            }

            case Assignment assignment:
            {
                var assignmentOutput = (AssignmentOutput) output;
                var formattedTime = FormatStatementTime(assignmentOutput.Time);
                if (assignmentOutput.Expression.IsComputed)
                {
                    var expressionValue = MppgOutput.OfValue(assignmentOutput.Expression);
                    Console.MarkupLineInterpolated(formattedTime.Concat($"{assignmentOutput.AssignedVariable} = [magenta]{expressionValue}[/]"));
                }
                else
                {
                    var expressionText = MppgOutput.OfExpression(
                        assignmentOutput.Expression, NancyOutput.OfExpression(assignmentOutput.Expression));
                    Console.MarkupLineInterpolated(formattedTime.Concat($"{assignmentOutput.AssignedVariable} = [magenta]{expressionText}[/]"));
                }
                break;
            }

            case Assertion assertion:
            {
                var assertionOutput = (AssertionOutput) output;
                Console.MarkupLineInterpolated(FormatStatementTime(assertionOutput.Time).Concat($"[magenta]{output.OutputText}[/]"));
                break;
            }

            case PropertyAssertion propertyAssertion:
            {
                var propertyAssertionOutput = (PropertyAssertionOutput) output;
                Console.MarkupLineInterpolated(FormatStatementTime(propertyAssertionOutput.Time).Concat($"[magenta]{output.OutputText}[/]"));
                break;
            }

            case Comment comment:
            {
                Console.MarkupLineInterpolated($"[green]{comment.Text}[/]");
                break;
            }

            case EmptyStatement es:
            {
                // do nothing
                break;
            }

            // must be matched before PlotCommand, of which it is a subtype
            case PlotTikzCommand plotTikz:
            {
                if(TikzPlotFormatter is not null)
                {
                    var plotOutput = (PlotOutput) output;
                    if (plotOutput.Time > TimeSpan.Zero && PrintTimePerStatement)
                    {
                        Console.MarkupLineInterpolated(FormatStatementTime(plotOutput.Time).Concat($"[grey]Plot inputs computed.[/]"));
                    }
                    TikzPlotFormatter.FormatTikzPlot(plotOutput);
                }
                else
                    Console.MarkupLineInterpolated($"[yellow]Plots disabled.[/]");
                break;
            }

            case PlotCommand plot:
            {
                if(PlotFormatter is not null)
                {
                    var plotOutput = (PlotOutput) output;
                    if (plotOutput.Time > TimeSpan.Zero && PrintTimePerStatement)
                    {
                        Console.MarkupLineInterpolated(FormatStatementTime(plotOutput.Time).Concat($"[grey]Plot inputs computed.[/]"));
                    }
                    PlotFormatter.FormatPlot(plotOutput);
                }
                else
                    Console.MarkupLineInterpolated($"[yellow]Plots disabled.[/]");
                break;
            }

            default:
            {
                Console.MarkupLineInterpolated($"{output.OutputText}");
                break;
            }
        }
    }

    /// <summary>
    /// If <see cref="PrintTimePerStatement"/> is true, formats the given timespan with markup.
    /// As it returns a FormattableString, the interpolation is not resolved.
    ///
    /// If <see cref="PrintTimePerStatement"/> is false, it returns an empty string intead.
    /// </summary>
    private FormattableString FormatStatementTime(TimeSpan time)
    {
        if (PrintTimePerStatement)
            return $"[blue][[{time}]][/] ";
        else
            return $"";
    }

    /// <summary>
    /// Writes the error the statement failed with, with the source line and a caret under it.
    /// </summary>
    public void FormatError(Statement statement, ErrorOutput error)
    {
        // On syntax errors, echo the input if it hasn't been echoed yet
        if (error.Exception is SyntaxErrorException && !EchoInput && !PrintInputAsConfirmation)
        {
            // Echo the input that caused the syntax error
            if (statement is not Comment and not EmptyStatement)
            {
                if (statement.InlineComment.IsNullOrWhiteSpace())
                    Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/]");
                else
                    Console.MarkupLineInterpolated($"[grey]» {statement.Text}[/] [green]{statement.InlineComment}[/]");
            }
        }

        switch(error.Exception)
        {
            case SyntaxErrorException { Error: { } syntaxError }:
            {
                Console.MarkupLine("[red]Syntax error[/]:");
                SyntaxErrorPrinter.PrintError(Console, syntaxError, "red", Verbose);
                break;
            }

            case SyntaxErrorException:
            {
                Console.MarkupLineInterpolated($"[red]Syntax error[/]: {error.Exception.Message}");
                break;
            }

            default:
            {
                Console.MarkupLineInterpolated($"[red]Execution error[/]: {error.Exception.Message}");
                break;
            }
        }

    }

    /// <summary>
    /// Writes that the program has ended.
    /// </summary>
    public void FormatEndOfProgram()
    {
        Console.MarkupLineInterpolated($"[yellow]End of Program.[/]");
    }
}