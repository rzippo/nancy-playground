using Spectre.Console;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;
using Unipi.Nancy.Plots.Tikz;

namespace Unipi.Nancy.Playground.Cli.Plots;

/// <summary>
/// Implements plotting using <a href="https://github.com/rzippo/nancy">Nancy.Plots.Tikz</a>.
/// The plot is rendered as TikZ code, to be compiled with LaTeX, rather than as an image.
/// The code is printed to the console, unless the <c>out</c> option is used to write it to file.
/// </summary>
public class TikzPlotFormatter
{
    /// <summary>
    /// The directory the files are written to.
    /// </summary>
    public ExportRoot PlotsExportRoot { get; set; }
    /// <summary>
    /// Where the code, or the path of the file it was written to, is printed.
    /// </summary>
    public IAnsiConsole Console { get; init; } = AnsiConsole.Console;

    /// <summary>
    /// A formatter writing its files to <paramref name="plotsRoot"/>.
    /// </summary>
    public TikzPlotFormatter(ExportRoot plotsRoot)
    {
        PlotsExportRoot = plotsRoot;
    }

    /// <summary>
    /// Writes the curves as TikZ code, to the console or to the file the command asked for.
    /// </summary>
    public void FormatTikzPlot(PlotOutput plotOutput)
    {
        if (plotOutput.FunctionsToPlot.Count == 0)
        {
            Console.MarkupLine("[red]No functions to plot.[/]");
            return;
        }

        var plotSettings = new TikzPlotSettings()
        {
            XLimit = plotOutput.XLimit.HasValue ?
                new Interval(plotOutput.XLimit.Value.Left, plotOutput.XLimit.Value.Right, true, true) :
                null,
            YLimit = plotOutput.YLimit.HasValue ?
                new Interval(plotOutput.YLimit.Value.Left, plotOutput.YLimit.Value.Right, true, true) :
                null,
        };
        // Nancy.Plots.Tikz has its own defaults for these, hence they are set only if the user asked for them
        if (!plotOutput.Title.IsNullOrWhiteSpace())
            plotSettings.Title = plotOutput.Title;
        if (!plotOutput.XLabel.IsNullOrWhiteSpace())
            plotSettings.XLabel = plotOutput.XLabel;
        if (!plotOutput.YLabel.IsNullOrWhiteSpace())
            plotSettings.YLabel = plotOutput.YLabel;

        var plotRenderer = new TikzNancyPlotRenderer() { PlotSettings = plotSettings };
        var curves = plotOutput.FunctionsToPlot
            .Select(pair => pair.Curve)
            .ToList();
        var names = plotOutput.FunctionsToPlot
            .Select(pair => pair.Name)
            .ToList();
        var tikzCode = plotRenderer.Plot(curves, names);

        var saveToFile = !plotOutput.Settings.OutPath.IsNullOrWhiteSpace();
        if (saveToFile)
        {
            var codePath = PlotsExportRoot.Resolve(plotOutput.Settings.OutPath);
            File.WriteAllText(codePath, tikzCode);
            Console.MarkupLineInterpolated($"[gray]Plot TikZ code written to: {codePath}[/]");
        }
        else
        {
            // the TikZ code is the output of the command:
            // it is written as-is, as it is full of characters that would be interpreted as markup
            Console.WriteLine(tikzCode);
        }
    }
}
