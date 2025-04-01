using Unipi.Nancy.Expressions;

namespace Unipi.MppgParser;

public class PlotCommand : Statement
{
    public List<Expression> FunctionsToPlot { get; init; } = [];
    public PlotSettings Settings { get; init; } = new();
    
    public override string Execute(State state)
    {
        return "Plotting is not implemented in this context.";
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        var functions = FunctionsToPlot
            .Select(ex =>
            {   
                ex.ParseTree(state);
                if(ex.NancyExpression is CurveExpression ce)
                    return ce.Compute();
                else
                    throw new Exception("Cannot plot a number.");
            })
            .ToList();
        return new PlotOutput
        {
            FunctionsToPlot = functions,
            Settings = Settings,
            StatementText = Text,
            OutputText = "If you are reading this, the formatter does not implement plots."
        };
    }
}