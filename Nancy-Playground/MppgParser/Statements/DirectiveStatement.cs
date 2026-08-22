namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// A '#!' directive other than the version one, which is kept but not acted upon.
/// </summary>
public record class DirectiveStatement : Statement
{
    /// <summary>
    /// Does nothing, an unknown directive having no effect.
    /// </summary>
    public override string Execute(State state)
    {
        return Text;
    }

    /// <summary>
    /// Does nothing, an unknown directive having no effect.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = Text,
            OutputText = Text
        };
    }
}
