using System.ComponentModel;
using System.Text;
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
        if (settings.Version)
        {
            AnsiConsole.MarkupLine(Program.CliVersionLine);
            return 0;
        }

        if (!settings.MuteWelcomeMessage)
            foreach (var cliWelcomeLine in Program.CliWelcomeMessage)
                AnsiConsole.MarkupLine(cliWelcomeLine);

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

        var programText = File.ReadAllText(mppgFile.FullName, Encoding.UTF8);
        var program = MppgParser.Program.FromText(programText);

        if (program.Errors.Count > 0)
        {
            if (settings.OnErrorMode == OnErrorMode.Stop)
            {
                AnsiConsole.MarkupLine("[red]ERROR! Syntax errors, run aborted:[/]");
                foreach(var error in program.Errors)
                    AnsiConsole.MarkupLineInterpolated($"[red]\t - {error.ToString()}[/]");
                return 1;
            }
            else
            {
                AnsiConsole.MarkupLine("[darkorange]WARNING! Syntax errors:[/]");
                foreach(var error in program.Errors)
                    AnsiConsole.MarkupLineInterpolated($"[darkorange]\t - {error.ToString()}[/]");
            }
        }

        var plotFormatter = settings.Deterministic ? null : new ScottPlotFormatter(plotsRoot);
        // add option to use XPlotPlotFormatter?

        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.ExplicitPrintsOnly => new OutputOnlyFormatter()
            {
                PlotFormatter = plotFormatter,
            },
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