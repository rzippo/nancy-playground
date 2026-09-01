using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

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
    /// Range for the x-axis, resolved to a value.
    /// Null if the command did not set one.
    /// </summary>
    public (Rational Left, Rational Right)? XLimit { get; init; } = null;
    /// <summary>
    /// Range for the y-axis, resolved to a value.
    /// Null if the command did not set one.
    /// </summary>
    public (Rational Left, Rational Right)? YLimit { get; init; } = null;
    /// <summary>
    /// The options given to the command, e.g. the range and the output file.
    /// </summary>
    public PlotSettings Settings { get; init; } = new();
    /// <summary>
    /// How long computing the curves took.
    /// </summary>
    public TimeSpan Time { get; init; } = TimeSpan.Zero;
}