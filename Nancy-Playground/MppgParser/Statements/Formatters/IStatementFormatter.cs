namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

/// <summary>
/// Writes what a statement produces, in the style and to the destination of the implementation.
/// </summary>
public interface IStatementFormatter
{
    /// <summary>
    /// Formats output before statement execution.
    /// Useful to output something before long commands.
    /// </summary>
    public void FormatStatementPreamble(Statement statement);
  
    /// <summary>
    /// Formats output after successful execution.
    /// </summary>
    public void FormatStatementOutput(Statement statement, StatementOutput output);

    /// <summary>
    /// Formats output of an error.
    /// </summary>
    public void FormatError(Statement statement, ErrorOutput error);

    /// <summary>
    /// Formats output for end of program reached.
    /// </summary>
    public void FormatEndOfProgram();
}