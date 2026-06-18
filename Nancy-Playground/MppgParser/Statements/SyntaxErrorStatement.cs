using Unipi.Nancy.Playground.MppgParser.Exceptions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class SyntaxErrorStatement : Statement
{
    public SyntaxErrorInfo? SyntaxError { get; init; }

    public Exception? InnerException { get; init; }

    public string Message { get; init; } = "Statement could not be parsed.";

    public override string Execute(State state)
    {
        throw CreateException();
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        throw CreateException();
    }

    private SyntaxErrorException CreateException()
    {
        var message = SyntaxError?.ToString() ?? Message;
        return InnerException is null
            ? new SyntaxErrorException(message)
            : new SyntaxErrorException(message, InnerException);
    }
}
