namespace Unipi.MppgParser;

public class PlotCommand : Statement
{
    public override string Execute(State state)
    {
        return "Not implemented";
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = Text,
            OutputText = "Not implemented"
        };
    }
}