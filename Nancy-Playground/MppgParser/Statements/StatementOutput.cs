namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// What a statement produced, which a formatter renders in its own style.
/// </summary>
public class StatementOutput
{
    /// <summary>
    /// Text of the statement.
    /// </summary>
    public required string StatementText { get; init; }

    /// <summary>
    /// Output text of the statement.
    /// </summary>
    /// <remarks>
    /// For statements that do not produce simply text, this should be populated as a fallback.
    /// </remarks>
    public required string OutputText { get; init; }

    /// <summary>
    /// The warnings raised while the statement ran, as against the ones <see cref="Statement.Warnings"/> holds from when it was parsed.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}