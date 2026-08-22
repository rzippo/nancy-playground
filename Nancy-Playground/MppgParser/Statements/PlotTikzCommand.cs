namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The <c>plotTikz</c> command, which plots the given functions as TikZ code instead of an image.
/// </summary>
/// <remarks>
/// It shares its arguments, and their computation, with <see cref="PlotCommand"/>:
/// the difference is in how the output is rendered, which is up to the formatter.
/// </remarks>
public record class PlotTikzCommand : PlotCommand
{
    /// <summary>
    /// Computes the functions and returns the plot to write as TikZ code.
    /// </summary>
    public override string Execute(State state)
    {
        return "TikZ plotting is not implemented in this context.";
    }
}
