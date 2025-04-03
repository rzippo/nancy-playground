using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.MppgParser;

public class PlotOutput : StatementOutput
{
    public List<(string Name, Curve Curve)> FunctionsToPlot { get; init; } = [];
    public List<string> FunctionNames { get; init; } = [];
    public PlotSettings Settings { get; init; } = new();
}