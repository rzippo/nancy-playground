namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The kind an expression resolves to.
/// </summary>
public enum ExpressionType
{
    /// <summary>
    /// A curve.
    /// </summary>
    Function,
    /// <summary>
    /// A number.
    /// </summary>
    Number,
    /// <summary>
    /// Neither yet, the expression not having been resolved.
    /// </summary>
    Undetermined
}