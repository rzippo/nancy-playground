namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class VersionDirectiveStatement : Statement
{
    /// <summary>
    /// The version the directive declares, or null if it declares one this build cannot apply.
    /// </summary>
    public SyntaxVersion? Version { get; init; }

    /// <summary>
    /// Why the directive cannot be applied, when <see cref="Version"/> is null.
    /// </summary>
    public string? Error { get; init; }

    public bool IsDuplicate { get; init; }

    public VersionDirectiveStatement(SyntaxVersion? version)
    {
        Version = version;
    }

    public override string Execute(State state)
    {
        if (Error is not null)
            return $"ERROR: {Error}";
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
