namespace Unipi.MppgParser;

public record class Comment : Statement
{
    public override string Execute(State state)
    {
        // todo: make optional?
        return Text;
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = Text,
            OutputText = Text
        };
    }
}