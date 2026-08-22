namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// A '#!syntax version' directive, which declares the version the rest of the program is written in.
/// </summary>
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

    /// <summary>
    /// True where an earlier directive already declared a version, this one having no effect.
    /// </summary>
    public bool IsDuplicate { get; init; }

    /// <summary>
    /// A directive declaring <paramref name="version"/>.
    /// </summary>
    public VersionDirectiveStatement(SyntaxVersion? version)
    {
        Version = version;
    }

    /// <summary>
    /// Reports why the directive was not applied, and nothing where it was.
    /// </summary>
    public override string Execute(State state)
    {
        if (Error is not null)
            return $"ERROR: {Error}";
        if (IsDuplicate)
            return $"WARNING: Duplicate syntax version directive. Only the first '#!syntax version X.Y' is applied. Active version: {Version}.";
        return string.Empty;
    }

    /// <summary>
    /// Reports why the directive was not applied, for a formatter to render.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        return new StatementOutput
        {
            StatementText = Text,
            OutputText = Execute(state)
        };
    }
}
