namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// A line that is only a comment, which the program keeps so that it can be written back.
/// </summary>
public record class Comment : Statement
{
    /// <summary>
    /// Does nothing, a comment having nothing to execute.
    /// </summary>
    public override string Execute(State state)
    {
        // todo: make optional?
        return Text;
    }

    /// <summary>
    /// Does nothing, a comment having nothing to execute.
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