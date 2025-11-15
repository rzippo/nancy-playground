namespace Unipi.MppgParser;

public class VariableNotFoundException : SyntaxErrorException
{
    public VariableNotFoundException()
    {
    }

    public VariableNotFoundException(string? message) : base(message)
    {
    }

    public VariableNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}