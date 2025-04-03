using System.Diagnostics;
using NancyMppg.Nancy.Plots;
using Spectre.Console;
using Unipi.MppgParser;

namespace NancyMppg;

public class HtmlPlotFormatter: IPlotFormatter
{
    public void FormatPlot(PlotOutput plotOutput)
    {
        if (plotOutput.FunctionsToPlot.Count == 0)
            AnsiConsole.MarkupLine("[red]No functions to plot.[/]");
        else
        {
            var curves = plotOutput.FunctionsToPlot
                .Select(pair => pair.Curve)
                .ToList();
            var names = plotOutput.FunctionsToPlot
                .Select(pair => pair.Name)
                .ToList();

            var plot = curves.Plot(names);
            
            var xlabel = string.IsNullOrWhiteSpace(plotOutput.Settings.XLabel) ?
                "x" : plotOutput.Settings.XLabel;
            var ylabel = string.IsNullOrWhiteSpace(plotOutput.Settings.YLabel) ?
                "y" : plotOutput.Settings.YLabel;
            
            plot.WithXTitle(xlabel);
            plot.WithYTitle(ylabel);

            // how to move the legend below?
            
            var html = plot.GetHtml();
            
            var htmlTempFileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".html";
            File.WriteAllText(htmlTempFileName, html);
            var psi = new ProcessStartInfo
            {
                FileName = htmlTempFileName,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}