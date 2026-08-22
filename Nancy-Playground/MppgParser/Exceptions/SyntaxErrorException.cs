namespace Unipi.Nancy.Playground.MppgParser.Exceptions;

/// <summary>
/// A program or a line that did not parse.
/// </summary>
public class SyntaxErrorException : Exception
{
    /// <summary>
    /// The structured description of the error, when it was collected from the parser.
    /// Null for errors raised outside parsing.
    /// </summary>
    public SyntaxErrorInfo? Error { get; init; }

    /// <summary>
    /// A syntax error with no message.
    /// </summary>
    public SyntaxErrorException()
    {
    }

    /// <summary>
    /// A syntax error reported as <paramref name="message"/>.
    /// </summary>
    public SyntaxErrorException(string? message) : base(message)
    {
    }

    /// <summary>
    /// A syntax error reported as <paramref name="message"/>, raised while handling <paramref name="innerException"/>.
    /// </summary>
    public SyntaxErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}