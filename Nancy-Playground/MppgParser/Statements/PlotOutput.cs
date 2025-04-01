using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.MppgParser;

public class PlotOutput : StatementOutput
{
    public List<Curve> FunctionsToPlot { get; init; } = [];
    public PlotSettings Settings { get; init; } = new();
}