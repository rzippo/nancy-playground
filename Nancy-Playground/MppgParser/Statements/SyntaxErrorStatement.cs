using Unipi.Nancy.Playground.MppgParser.Exceptions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// A line that did not parse, kept in place of the statement so that the program can report it where it stands.
/// </summary>
public record class SyntaxErrorStatement : Statement
{
    /// <summary>
    /// The error found, where the parser reported one.
    /// </summary>
    public SyntaxErrorInfo? SyntaxError { get; init; }

    /// <summary>
    /// The exception the parse failed with, where there is one.
    /// </summary>
    public Exception? InnerException { get; init; }

    /// <summary>
    /// What to report when there is no error to describe.
    /// </summary>
    public string Message { get; init; } = "Statement could not be parsed.";

    /// <summary>
    /// Throws the error the line failed with, so that running the program reports it.
    /// </summary>
    public override string Execute(State state)
    {
        throw CreateException();
    }

    /// <summary>
    /// Throws the error the line failed with, so that running the program reports it.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        throw CreateException();
    }

    private SyntaxErrorException CreateException()
    {
        var message = SyntaxError?.ToString() ?? Message;
        return InnerException is null
            ? new SyntaxErrorException(message) { Error = SyntaxError }
            : new SyntaxErrorException(message, InnerException) { Error = SyntaxError };
    }
}
