using Unipi.Nancy.Expressions;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The <c>plot</c> command, which draws the given functions.
/// </summary>
public record class PlotCommand : Statement
{
    /// <summary>
    /// The functions to draw.
    /// </summary>
    public List<Expression> FunctionsToPlot { get; init; } = [];
    /// <summary>
    /// The options given to the command, e.g. the range and the output file.
    /// </summary>
    public PlotSettings Settings { get; init; } = new();
    
    /// <summary>
    /// Computes the functions and returns the plot to draw.
    /// </summary>
    public override string Execute(State state)
    {
        return "Plotting is not implemented in this context.";
    }

    /// <summary>
    /// Computes the functions and returns the plot to draw, for a formatter to render.
    /// </summary>
    public override StatementOutput ExecuteToFormattable(State state)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var functions = FunctionsToPlot
            .Select(ex =>
            {   
                ex.ParseTree(state);
                if(ex.NancyExpression is CurveExpression ce)
                    return (ce.Name, ce.Compute());
                else
                    throw new Exception("Cannot plot a number.");
            })
            .ToList();
        var title = Settings.Title.Compute(state);
        var xLabel = Settings.XLabel.Compute(state);
        var yLabel = Settings.YLabel.Compute(state);

        stopwatch.Stop();

        return new PlotOutput
        {
            FunctionsToPlot = functions,
            Title = title,
            XLabel = xLabel,
            YLabel = yLabel,
            Settings = Settings,
            Time = stopwatch.Elapsed,
            StatementText = Text,
            // Plots produce no text; the formatter renders them, or explains that it cannot.
            OutputText = string.Empty
        };
    }
}