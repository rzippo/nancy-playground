namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The output of a statement that failed, i.e. the error to report.
/// </summary>
public class ErrorOutput : StatementOutput
{
    /// <summary>
    /// The exception emitted for the error.
    /// </summary>
    public required Exception Exception { get; init; }
}