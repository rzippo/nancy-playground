using Spectre.Console;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Only prints out the result of explicit print requests.
/// </summary>
public class OutputOnlyFormatter : IStatementFormatter
{
    /// <summary>
    /// What draws a plot, or null where none is drawn.
    /// </summary>
    public IPlotFormatter? PlotFormatter { get; init; }
    /// <summary>
    /// What writes a TikZ plot, or null where none is written.
    /// </summary>
    public TikzPlotFormatter? TikzPlotFormatter { get; init; }
    /// <summary>
    /// Where the output is written.
    /// </summary>
    public IAnsiConsole Console { get; init; } = AnsiConsole.Console;

    /// <summary>
    /// Writes nothing, this style announcing no statement before it runs.
    /// </summary>
    public void FormatStatementPreamble(Statement statement)
    {
        return;
    }

    /// <summary>
    /// Writes what the statement produced, and nothing where it produced no value.
    /// </summary>
    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        // A diagnostic is about correctness, so it is printed even in the mode that otherwise prints only what the script explicitly asked for.
        foreach (var warning in statement.Warnings)
            Console.MarkupLineInterpolated($"[yellow]{warning}[/]");

        switch (statement)
        {
            case ExpressionCommand expression:
            {
                var expressionOutput = (ExpressionOutput)output;
                // assume we are being *required* to compute the expression
                // this mode is the one compared against a converted program, so it prints as Nancy does
                Console.WriteLine(NancyOutput.OfValue(expressionOutput.Expression));
                break;
            }
            
            // must be matched before PlotCommand, of which it is a subtype
            case PlotTikzCommand plotTikz:
            {
                if(TikzPlotFormatter is not null)
                    // we do not control the output of the TikzPlotFormatter
                    TikzPlotFormatter.FormatTikzPlot((PlotOutput) output);
                break;
            }

            case PlotCommand plot:
            {
                if(PlotFormatter is not null)
                    // we do not control the output of the PlotFormatter
                    PlotFormatter.FormatPlot((PlotOutput) output);
                break;
            }

            case Assertion:
            {
                var assertionOutput = (AssertionOutput)output;
                Console.WriteLine(assertionOutput.Result.ToString().ToLower());
                break;
            }

            // all other outputs are suppressed
            default:
                break;
        }
    }

    /// <summary>
    /// Writes the error the statement failed with.
    /// </summary>
    public void FormatError(Statement statement, ErrorOutput error)
    {
        return;
    }

    /// <summary>
    /// Writes nothing, this style announcing no end of program.
    /// </summary>
    public void FormatEndOfProgram()
    {
        return;
    }
}