namespace Unipi.Nancy.Playground.MppgParser.Exceptions;

/// <summary>
/// A name used where no variable of that name is declared.
/// </summary>
public class VariableNotFoundException : SyntaxErrorException
{
    /// <summary>
    /// An unknown variable with no message.
    /// </summary>
    public VariableNotFoundException()
    {
    }

    /// <summary>
    /// An unknown variable reported as <paramref name="message"/>.
    /// </summary>
    public VariableNotFoundException(string? message) : base(message)
    {
    }

    /// <summary>
    /// An unknown variable reported as <paramref name="message"/>, raised while handling <paramref name="innerException"/>.
    /// </summary>
    public VariableNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}