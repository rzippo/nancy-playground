namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class VersionDirectiveStatement : Statement
{
    public SyntaxVersion Version { get; init; }

    public bool IsDuplicate { get; init; }

    public VersionDirectiveStatement(SyntaxVersion version)
    {
        Version = version;
    }

    public override string Execute(State state)
    {
        if (IsDuplicate)
            return $"WARNING: Duplicate syntax version directive. Only the first '#!syntax version X.Y' is applied. Active version: {Version}.";
        return string.Empty;
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = Text,
            OutputText = Execute(state)
        };
    }
}
