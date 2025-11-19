using Unipi.MppgParser;

namespace NancyMppg;

public interface IPlotFormatter
{
    public string PlotsExportRoot { get; set; }

    public void FormatPlot(PlotOutput plotOutput);
}