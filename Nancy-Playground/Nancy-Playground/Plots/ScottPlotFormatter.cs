using System.Diagnostics;
using Spectre.Console;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Playground.Cli.Nancy.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli.Plots;

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
            
            var curves = Enumerable
                .Select<(string Name, Curve Curve), Curve>(plotOutput.FunctionsToPlot, pair => pair.Curve)
                .ToList();
            var names = Enumerable
                .Select<(string Name, Curve Curve), string>(plotOutput.FunctionsToPlot, pair => pair.Name)
                .ToList();

            var xLimit = plotOutput.Settings.XLimit;
            var yLimit = plotOutput.Settings.YLimit;

            var plot = xLimit.HasValue ?
                plotter.Plot(curves, names, xLimit.Value.Left, xLimit.Value.Right) :
                plotter.Plot(curves, names);

            var xLabel = string.IsNullOrWhiteSpace(plotOutput.XLabel) ?
                "x" : plotOutput.XLabel;
            var yLabel = string.IsNullOrWhiteSpace(plotOutput.YLabel) ?
                "y" : plotOutput.YLabel;

            plot.XLabel(xLabel);
            plot.YLabel(yLabel);

            if (xLimit.HasValue)
            {
                var (left, right) = xLimit.Value;
                plot.Axes.SetLimitsX((double)left, (double)right);
            }

            if (yLimit.HasValue)
            {
                var (left, right) = yLimit.Value;
                plot.Axes.SetLimitsY((double)left, (double)right);
            }

            if (!string.IsNullOrWhiteSpace(plotOutput.Title))
            {
                // AnsiConsole.MarkupLine($"[red]Setting title: {plotOutput.Title}[/]");
                plot.Title(plotOutput.Title);
            }

            // default behavior: open a GUI window or tab to show the plot; it will not be interactive
            var showInGui = plotOutput.Settings.ShowInGui ?? true;
            var saveToFile = !plotOutput.Settings.OutPath.IsNullOrWhiteSpace();

            if (saveToFile)
            {
                var imagePath = Path.Join(PlotsExportRoot, (string?)plotOutput.Settings.OutPath);
                var imageBytes = plotter.GetImage(plot);
                File.WriteAllBytes(imagePath, imageBytes);

                if (showInGui)
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = imagePath,
                        UseShellExecute = true
                    };
                    try {
                        Process.Start(psi);
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Unable to open plot in GUI.[/] [gray]Is this a container?[/]");
                    }
                }
            }
            else if (showInGui)
            {
                var imageBytes = plotter.GetImage(plot);
                var imageTempFileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";
                File.WriteAllBytes(imageTempFileName, imageBytes);
                AnsiConsole.MarkupLine($"[gray]Temp image written to: {imageTempFileName}; opening in default app[/]");
                var psi = new ProcessStartInfo
                {
                    FileName = imageTempFileName,
                    UseShellExecute = true
                };
                try {
                    Process.Start(psi);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    AnsiConsole.MarkupLine($"[yellow]Unable to open plot in GUI.[/] [gray]Is this a container?[/]");
                }
            }
        }
    }
}