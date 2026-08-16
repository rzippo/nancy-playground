using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

/// <summary>
/// The output style of the original console, which writes a value as the syntax writes it.
/// </summary>
public class PlainConsoleStatementFormatter: IStatementFormatter
{
    /// <summary>
    /// Where the output is written, or null to write to the console as it is when writing, rather
    /// than as it was when this formatter was built.
    /// </summary>
    public TextWriter? Out { get; init; }

    private TextWriter Writer => Out ?? Console.Out;

    public void FormatStatementPreamble(Statement statement)
    {
        if(statement is not Comment)
            Writer.WriteLine(statement.Text);
    }

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

    public void FormatError(Statement statement, ErrorOutput error)
    {
        Writer.WriteLine($"Error: {error.Exception.Message}");
    }

    public void FormatEndOfProgram()
    {
        Writer.WriteLine(">> end of program");
    }
}