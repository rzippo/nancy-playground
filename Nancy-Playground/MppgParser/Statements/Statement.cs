namespace Unipi.MppgParser;

public abstract class Statement
{
    public string Text { get; init; } = string.Empty;

    public abstract string Execute(State state);
}