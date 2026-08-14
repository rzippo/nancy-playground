namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

public class PlainConsoleStatementFormatter: IStatementFormatter
{
    public void FormatStatementPreamble(Statement statement)
    {
        if(statement is not Comment)
            Console.WriteLine(statement.Text);
    }

    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        foreach (var warning in statement.Warnings)
            Console.WriteLine(warning);

        if (output is PlotOutput)
            Console.WriteLine(">> Plots are not rendered in this output mode.");
        else
            Console.WriteLine($">> {output.OutputText}");
    }

    public void FormatError(Statement statement, ErrorOutput error)
    {
        Console.WriteLine($"Error: {error.Exception.Message}");
    }

    public void FormatEndOfProgram()
    {
        Console.WriteLine(">> end of program");
    }
}