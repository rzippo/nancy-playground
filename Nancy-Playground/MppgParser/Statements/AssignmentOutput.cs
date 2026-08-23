namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The output of an assignment, i.e. the value the name now holds.
/// </summary>
public class AssignmentOutput : ExpressionOutput
{
    /// <summary>
    /// The variable that was assigned.
    /// </summary>
    public required string AssignedVariable { get; init; }
}