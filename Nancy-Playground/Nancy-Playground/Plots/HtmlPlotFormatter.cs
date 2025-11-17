using System.Diagnostics;
using NancyMppg.Nancy.Plots;
using Spectre.Console;
using Unipi.MppgParser;
using Unipi.MppgParser.Utility;

namespace NancyMppg;

public class HtmlPlotFormatter: IPlotFormatter
{
    public string PlotsRoot { get; set; }

    public HtmlPlotFormatter(string? plotsRoot)
    {
        PlotsRoot = string.IsNullOrWhiteSpace(plotsRoot) ?
            Environment.CurrentDirectory :
            plotsRoot;
    }

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

            if (plotOutput.Settings.ShowInBrowser)
            {
                var htmlTempFileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".html";
                File.WriteAllText(htmlTempFileName, html);
                AnsiConsole.MarkupLine($"[gray]Html written to: {htmlTempFileName}; opening in default browser[/]");
                var psi = new ProcessStartInfo
                {
                    FileName = htmlTempFileName,
                    UseShellExecute = true
                };
                try {
                    Process.Start(psi);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    AnsiConsole.MarkupLine($"[yellow]Unable to open plot in browser.[/] [gray]Is this a container?[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[gray]In-browser plot skipped.[/]");
            }

            if (!plotOutput.Settings.OutPath.IsNullOrWhiteSpace())
            {
                var imagePath = Path.Join(PlotsRoot, plotOutput.Settings.OutPath);
                byte[] imageBytes;
                try
                {
                    imageBytes = HtmlToImage.RenderAsync(html).Result;
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