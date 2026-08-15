namespace Unipi.Nancy.Playground.MppgParser.Exceptions;

public class SyntaxErrorException : Exception
{
    /// <summary>
    /// The structured description of the error, when it was collected from the parser.
    /// Null for errors raised outside parsing.
    /// </summary>
    public SyntaxErrorInfo? Error { get; init; }

    public SyntaxErrorException()
    {
    }

    public SyntaxErrorException(string? message) : base(message)
    {
    }

    public SyntaxErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}