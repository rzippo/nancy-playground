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
    /// True where the directive was not applied, i.e. it does not open the program.
    /// </summary>
    public bool IsDuplicate { get; init; }

    /// <summary>
    /// The version the program is read with, which a directive that was not applied did not set.
    /// </summary>
    public SyntaxVersion? ActiveVersion { get; init; }

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
            return "WARNING: This syntax version directive is not applied. "
                + $"Only one that opens the program is. Active version: {ActiveVersion ?? Version}.";
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
