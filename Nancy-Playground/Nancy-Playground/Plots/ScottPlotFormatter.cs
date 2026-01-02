using System.Diagnostics;
using Spectre.Console;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.Cli.Nancy.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;
using Unipi.Nancy.Plots.ScottPlot;

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
            var plotSettings = new ScottPlotSettings()
            {
                Title = plotOutput.Title,
                XLabel = plotOutput.XLabel,
                YLabel = plotOutput.YLabel,
                XLimit = plotOutput.Settings.XLimit.HasValue ?
                    new Interval(plotOutput.Settings.XLimit.Value.Left, plotOutput.Settings.XLimit.Value.Right, true, true) :
                    null,
                YLimit = plotOutput.Settings.YLimit.HasValue ?
                    new Interval(plotOutput.Settings.YLimit.Value.Left, plotOutput.Settings.YLimit.Value.Right, true, true) :
                    null,
            };
            
            var plotRenderer = new ScottNancyPlotRenderer() { PlotSettings = plotSettings };
            var curves = Enumerable
                .Select<(string Name, Curve Curve), Curve>(plotOutput.FunctionsToPlot, pair => pair.Curve)
                .ToList();
            var names = Enumerable
                .Select<(string Name, Curve Curve), string>(plotOutput.FunctionsToPlot, pair => pair.Name)
                .ToList();
            var imageBytes = plotRenderer.Plot(curves, names);

            // default behavior: open a GUI window or tab to show the plot; it will not be interactive
            var showInGui = plotOutput.Settings.ShowInGui ?? true;
            var saveToFile = !plotOutput.Settings.OutPath.IsNullOrWhiteSpace();

            if (saveToFile)
            {
                var imagePath = Path.Join(PlotsExportRoot, (string?)plotOutput.Settings.OutPath);
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