using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
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
    /// <summary>
    /// The directory the images are written to.
    /// </summary>
    public ExportRoot PlotsExportRoot { get; set; }
    /// <summary>
    /// Where the path of each image is printed.
    /// </summary>
    public IAnsiConsole Console { get; init; } = AnsiConsole.Console;
    /// <summary>
    /// True to open each image in a window as it is written.
    /// </summary>
    public bool AutoOpenPlots { get; init; } = true;

    /// <summary>
    /// A formatter writing its images to <paramref name="plotsRoot"/>.
    /// </summary>
    public ScottPlotFormatter(ExportRoot plotsRoot)
    {
        PlotsExportRoot = plotsRoot;
    }

    /// <summary>
    /// Draws the curves as an image, and opens it where the settings ask.
    /// </summary>
    public void FormatPlot(PlotOutput plotOutput)
    {
        if (plotOutput.FunctionsToPlot.Count == 0)
            Console.MarkupLine("[red]No functions to plot.[/]");
        else
        {
            var plotSettings = new ScottPlotSettings()
            {
                Title = plotOutput.Title,
                XLabel = plotOutput.XLabel,
                YLabel = plotOutput.YLabel,
                XLimit = plotOutput.XLimit.HasValue ?
                    new Interval(plotOutput.XLimit.Value.Left, plotOutput.XLimit.Value.Right, true, true) :
                    null,
                YLimit = plotOutput.YLimit.HasValue ?
                    new Interval(plotOutput.YLimit.Value.Left, plotOutput.YLimit.Value.Right, true, true) :
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

            var imagePath = saveToFile ?
                PlotsExportRoot.Resolve(plotOutput.Settings.OutPath) :
                Path.GetTempPath() + Guid.NewGuid().ToString() + ".png";
            File.WriteAllBytes(imagePath, imageBytes);

            Console.MarkupLine($"[gray]Plot image written to: {imagePath}[/]");

            if (showInGui)
            {
                if(!AutoOpenPlots)
                {
                    Console.MarkupLine($"[yellow]GUI disabled with --no-gui, the gui option of this plot is ignored.[/]");
                    return;
                }

                var command = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "xdg-open" :
                  RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" :
                  RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "xdg-open";
                var args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new[] { "/c", "start", imagePath } : new[] { imagePath };

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args.JoinText(),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                try {
                    Process.Start(psi);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    Console.MarkupLine($"[yellow]Unable to open plot in GUI.[/] [gray]Is this a container?[/]");
                }
            }
        }
    }
}