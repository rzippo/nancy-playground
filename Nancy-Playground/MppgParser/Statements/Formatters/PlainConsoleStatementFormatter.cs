using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

/// <summary>
/// The output style of the original console, which writes a value as the syntax writes it.
/// Depends on nothing but <see cref="Console"/>, for a consumer of the parser that has no console
/// library: the CLI has its own, which writes to the console it was given.
/// </summary>
public class PlainConsoleStatementFormatter: IStatementFormatter
{
    private static TextWriter Writer => Console.Out;

    /// <summary>
    /// Echoes the statement before it runs, unless it is a comment.
    /// </summary>
    public void FormatStatementPreamble(Statement statement)
    {
        if(statement is not Comment)
            Writer.WriteLine(statement.Text);
    }

    /// <summary>
    /// Writes what the statement produced.
    /// </summary>
    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        foreach (var warning in statement.Warnings)
            Writer.WriteLine(warning);

        if (output is PlotOutput)
            Writer.WriteLine(">> Plots are not rendered in this output mode.");
        else
            // the output text is already in the default notation, which is the syntax itself
            Writer.WriteLine($">> {output.OutputText}");
    }

    /// <summary>
    /// Writes the error the statement failed with.
    /// </summary>
    public void FormatError(Statement statement, ErrorOutput error)
    {
        Writer.WriteLine($"Error: {error.Exception.Message}");
    }

    /// <summary>
    /// Writes that the program has ended.
    /// </summary>
    public void FormatEndOfProgram()
    {
        Writer.WriteLine(">> end of program");
    }
}