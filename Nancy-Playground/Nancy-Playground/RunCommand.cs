using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.Cli;

public class RunCommand : Command<RunCommand.Settings>
{
    public sealed class Settings : CommonExecutionSettings
    {
        [Description("Path to the .mppg file to run")]
        [CommandArgument(0, "<file>")]
        public string MppgFile { get; init; } = string.Empty;
        
        [Description("If enabled, makes the output deterministic, removing preamble and time measurements. Useful to implement tests.")]
        [CommandOption("--deterministic")]
        public bool Deterministic { get; init; } = false;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.MppgFile))
        {
            AnsiConsole.MarkupLine($"[red]No input file specified.[/]");
            AnsiConsole.MarkupLine($"[red]Did you want to run the interactive command?[/]");
            return 1;
        }
        
        var mppgFile = new FileInfo(settings.MppgFile);
        if (!mppgFile.Exists)
        {
            AnsiConsole.MarkupLine($"[red]{mppgFile.FullName}: file not found.[/]");
            return 1;
        }

        // todo: make this configurable
        var plotsRoot = mppgFile.Directory?.FullName;
        if(!settings.Deterministic)
            AnsiConsole.MarkupLine($"[yellow]Plots will be saved in: {plotsRoot}[/]");

        var programText = File.ReadAllText(mppgFile.FullName);
        var program = MppgParser.Program.FromText(programText);

        var plotFormatter = settings.Deterministic ? null : new ScottPlotFormatter(plotsRoot);
        // add option to use XPlotPlotFormatter?
        
        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.MppgClassic => new PlainConsoleStatementFormatter(),
            OutputMode.NancyNew => new AnsiConsoleStatementFormatter()
            {
                PlotFormatter = plotFormatter,
                PrintTimePerStatement = !settings.Deterministic,
            },
            _ => new PlainConsoleStatementFormatter()
        };

        var immediateComputeValue = settings.RunMode switch
        {
            RunMode.ExpressionDriven => false,
            RunMode.PerStatement => true,
            _ => false
        };

        var totalComputationTime = TimeSpan.Zero;
        while (!program.IsEndOfProgram)
        {
            var output = program.ExecuteNextStatement(formatter, immediateComputeValue);
            if(output is ExpressionOutput expressionOutput)
                totalComputationTime += expressionOutput.Time;
            if (settings.OnErrorMode == OnErrorMode.Stop &&
                output is ErrorOutput)
                break;
        }

        // use formatter?
        if(!settings.Deterministic)
            Console.WriteLine($"Total computation time: {totalComputationTime}");
        return 0;
    }
}