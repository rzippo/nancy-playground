using NancyMppg.Nancy.Plots;
using Spectre.Console;
using Unipi.MppgParser;
using Unipi.MppgParser.Utility;

namespace NancyMppg;

/// <summary>
/// Implements plotting using <a href="https://github.com/ScottPlot/ScottPlot">ScottPlot</a>
/// Pros: should produce good image exports.
/// Cons: performance in interactive contexts, such as browser, is unsure.
/// </summary>
public class ScottPlotFormatter : IPlotFormatter
{
    public string PlotsExportRoot { get; set; }

    public ScottPlotFormatter(string? plotsRoot)
    {
        PlotsExportRoot = string.IsNullOrWhiteSpace(plotsRoot) ?
            Environment.CurrentDirectory :
            plotsRoot;
    }

    public void FormatPlot(PlotOutput plotOutput)
    {
        if (plotOutput.FunctionsToPlot.Count == 0)
            AnsiConsole.MarkupLine("[red]No functions to plot.[/]");
        else
        {
            var plotter = new ScottPlotNancyPlotter();
            
            var curves = plotOutput.FunctionsToPlot
                .Select(pair => pair.Curve)
                .ToList();
            var names = plotOutput.FunctionsToPlot
                .Select(pair => pair.Name)
                .ToList();

            var plot = plotter.Plot(curves, names);

            var xlabel = string.IsNullOrWhiteSpace(plotOutput.Settings.XLabel) ?
                "x" : plotOutput.Settings.XLabel;
            var ylabel = string.IsNullOrWhiteSpace(plotOutput.Settings.YLabel) ?
                "y" : plotOutput.Settings.YLabel;

            plot.XLabel(xlabel);
            plot.YLabel(ylabel);

            if (!string.IsNullOrWhiteSpace(plotOutput.Title))
            {
                // AnsiConsole.MarkupLine($"[red]Setting title: {plotOutput.Title}[/]");
                plot.Title(plotOutput.Title);
            }

            // default behavior: do NOT open a browser tab to show the interactive plot
            var showInBrowser = plotOutput.Settings.ShowInBrowser ?? false;

            if (showInBrowser)
            {
                // todo: implement
            }
            else
            {
                AnsiConsole.MarkupLine($"[gray]In-browser plot skipped.[/]");
            }

            if (!plotOutput.Settings.OutPath.IsNullOrWhiteSpace())
            {
                var imagePath = Path.Join(PlotsExportRoot, plotOutput.Settings.OutPath);
                byte[] imageBytes;
                try
                {
                    imageBytes = plotter.GetImage(plot);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine("Image rendering failed. May be due to dependencies: try running [yellow]nancy-playground setup[/]");
                    Console.WriteLine(e.Message);
                    return;
                }

                File.WriteAllBytes(imagePath, imageBytes);
            }
        }
    }
}