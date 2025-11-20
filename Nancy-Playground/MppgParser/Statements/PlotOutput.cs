using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.MppgParser;

public class PlotOutput : StatementOutput
{
    public List<(string Name, Curve Curve)> FunctionsToPlot { get; init; } = [];
    public string Title { get; init; } = string.Empty;
    public string XLabel { get; init; } = string.Empty;
    public string YLabel { get; init; } = string.Empty;
    public PlotSettings Settings { get; init; } = new();
}