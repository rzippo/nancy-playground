namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class DirectiveStatement : Statement
{
    public override string Execute(State state)
    {
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
