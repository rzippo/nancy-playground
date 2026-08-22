using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.Cli.Plots;

/// <summary>
/// Draws what a plot command produced, in the form and to the destination of the implementation.
/// </summary>
public interface IPlotFormatter
{
    /// <summary>
    /// The directory the plots are written to.
    /// </summary>
    public ExportRoot PlotsExportRoot { get; set; }

    /// <summary>
    /// Draws <paramref name="plotOutput"/>.
    /// </summary>
    public void FormatPlot(PlotOutput plotOutput);
}