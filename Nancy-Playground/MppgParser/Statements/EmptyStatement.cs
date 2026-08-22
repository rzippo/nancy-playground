namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// Empty statement are supported for compatibility, but should be effectively ignored.
/// </summary>
public record class EmptyStatement : Statement
{
    /// <summary>
    /// Does nothing, an empty line having nothing to execute.
    /// </summary>
    public override string Execute(State state)
    {
        return string.Empty;
    }

    /// <summary>
    /// Does nothing, an empty line having nothing to execute.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = string.Empty,
            OutputText = string.Empty
        };
    }
}