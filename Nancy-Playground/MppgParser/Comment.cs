namespace Unipi.MppgParser;

public class Comment : Statement
{
    public override string Execute(State state)
    {
        // todo: make optional?
        return Text;
    }
}