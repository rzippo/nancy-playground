using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser;

public class AssignmentOutput : ExpressionOutput
{
    /// <summary>
    /// The variable that was assigned.
    /// </summary>
    public required string AssignedVariable { get; init; }
}