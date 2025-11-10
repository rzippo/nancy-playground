using Unipi.Nancy.Numerics;

namespace Unipi.MppgParser;

public record PlotSettings
{
    /// <summary>
    /// The graph title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Range for the x-axis.
    /// </summary>
    public (Rational?, Rational?) XLimit { get; init; } = (null, null);

    /// <summary>
    /// Range for the y-axis.
    /// </summary>
    public (Rational?, Rational?) YLimit { get; init; } = (null, null);

    /// <summary>
    /// Label for the x-axis.
    /// </summary>
    public string XLabel { get; init; } = string.Empty;

    /// <summary>
    /// Label for the y-axis.
    /// </summary>
    public string YLabel { get; init; } = string.Empty;

    /// <summary>
    /// Name of the png file to save the plot to.
    /// </summary>
    public string OutPath { get; init; } = string.Empty;

    /// <summary>
    /// If false, removes the grid from the plot.
    /// </summary>
    public bool ShowGrid { get; init; } = true;

    /// <summary>
    /// If false, remove the background from the plot.
    /// </summary>
    public bool ShowBackground { get; init; } = true;

    /// <summary>
    /// If false, the plot is NOT shown in the browser.
    /// </summary>
    public bool ShowInBrowser { get; init; } = true;
}