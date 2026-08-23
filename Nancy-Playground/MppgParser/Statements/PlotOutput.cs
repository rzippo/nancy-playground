using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// A plot to be drawn, i.e. the curves and the options the command was given.
/// </summary>
public class PlotOutput : StatementOutput
{
    /// <summary>
    /// The curves to draw, each with the name to label it with.
    /// </summary>
    public List<(string Name, Curve Curve)> FunctionsToPlot { get; init; } = [];
    /// <summary>
    /// The title of the plot.
    /// </summary>
    public string Title { get; init; } = string.Empty;
    /// <summary>
    /// The label of the horizontal axis.
    /// </summary>
    public string XLabel { get; init; } = string.Empty;
    /// <summary>
    /// The label of the vertical axis.
    /// </summary>
    public string YLabel { get; init; } = string.Empty;
    /// <summary>
    /// The options given to the command, e.g. the range and the output file.
    /// </summary>
    public PlotSettings Settings { get; init; } = new();
    /// <summary>
    /// How long computing the curves took.
    /// </summary>
    public TimeSpan Time { get; init; } = TimeSpan.Zero;
}