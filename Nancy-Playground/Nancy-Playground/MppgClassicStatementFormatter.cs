using Spectre.Console;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// The output style of the original console, which writes a value as the syntax writes it.
/// Writes through <see cref="AnsiConsoleExtensions.WriteLine(IAnsiConsole, string)"/>, which parses no
/// markup, so the text comes out as it is given.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="PlainConsoleStatementFormatter"/>, which writes to the console of the
/// system and is what a consumer of the parser gets without a console library.
/// This one has the console of the CLI, so its output is captured by the tests, and its errors are
/// rendered like those of every other mode.
/// </remarks>
public class MppgClassicStatementFormatter : IStatementFormatter
{
    /// <summary>
    /// True for verbose output, meant for debugging.
    /// </summary>
    public bool Verbose { get; init; } = false;

    /// <summary>
    /// Where the output is written.
    /// </summary>
    public required IAnsiConsole Console { get; init; }

    /// <summary>
    /// Echoes the statement before it runs, unless it is a comment.
    /// </summary>
    public void FormatStatementPreamble(Statement statement)
    {
        if (statement is not Comment)
            Console.WriteLine(statement.Text);
    }

    /// <summary>
    /// Writes what the statement produced.
    /// </summary>
    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        foreach (var warning in statement.Warnings)
            Console.WriteLine(warning);

        if (output is PlotOutput)
            Console.WriteLine(">> Plots are not rendered in this output mode.");
        else
            // the output text is already in the default notation, which is the syntax itself
            Console.WriteLine($">> {output.OutputText}");
    }

    /// <summary>
    /// Writes the error the statement failed with.
    /// </summary>
    public void FormatError(Statement statement, ErrorOutput error)
    {
        if (error.Exception is SyntaxErrorException { Error: { } syntaxError })
        {
            Console.WriteLine("Error:");
            SyntaxErrorPrinter.PrintError(Console, syntaxError, "default", Verbose);
        }
        else
        {
            Console.WriteLine($"Error: {error.Exception.Message}");
        }
    }

    /// <summary>
    /// Writes that the program has ended.
    /// </summary>
    public void FormatEndOfProgram()
    {
        Console.WriteLine(">> end of program");
    }
}
