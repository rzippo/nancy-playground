using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.Nancy.Playground.Cli.Plots;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Specifies where plot output files should be saved.
/// </summary>
public enum PlotRootMode
{
    /// <summary>Save plots in the same directory as the MPPG script file.</summary>
    ScriptDirectory,
    
    /// <summary>Save plots in the current working directory.</summary>
    CurrentDirectory,
    
    /// <summary>Save plots in a manually specified directory.</summary>
    Custom,
}

/// <summary>
/// The 'run' command, which runs an MPPG program from start to end.
/// </summary>
public class RunCommand : Command<RunCommand.Settings>
{
    private IAnsiConsole Console {get; init;} = AnsiConsole.Console;

    /// <summary>
    /// The options of the 'run' command.
    /// </summary>
    public sealed class Settings : CommonExecutionSettings
    {
        /// <summary>
        /// The program to run.
        /// </summary>
        [Description("Path to the .mppg file to run")]
        [CommandArgument(0, "<file>")]
        public string MppgFile { get; init; } = string.Empty;

        /// <summary>
        /// True to make the output repeatable, which leaves out the preamble and the timings.
        /// </summary>
        [Description("If enabled, makes the output deterministic, removing preamble and time measurements. Useful to implement tests.")]
        [CommandOption("--deterministic")]
        public bool Deterministic { get; init; } = false;

        /// <summary>
        /// Where the plots of the program are written, chosen among the modes rather than as a path.
        /// </summary>
        [Description("Where to save plot output files. Options: ScriptDirectory (default), CurrentDirectory, or Custom. If --plots-root is specified, this defaults to Custom and must not be anything else.")]
        [CommandOption("--plots-root-mode")]
        public PlotRootMode? PlotsRootMode { get; init; }

        /// <summary>
        /// The directory the plots are written to, where the mode calls for one.
        /// </summary>
        [Description("Explicit directory for saving plot files. If specified, --plots-root-mode is assumed to be Custom.")]
        [CommandOption("--plots-root")]
        public string? PlotsRoot { get; init; }

    }

    /// <summary>
    /// A command writing to <paramref name="console"/>.
    /// </summary>
    public RunCommand(IAnsiConsole console)
    {
        Console = console;
    }

    /// <summary>
    /// Runs the program and writes its output, returning the exit code of the command.
    /// </summary>
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Version)
        {
            Console.MarkupLine(Program.CliVersionLine);
            return 0;
        }

        if (!settings.MuteWelcomeMessage)
            foreach (var cliWelcomeLine in Program.CliWelcomeMessage)
                Console.MarkupLine(cliWelcomeLine);

        if (string.IsNullOrWhiteSpace(settings.MppgFile))
        {
            Console.MarkupLine($"[red]No input file specified.[/]");
            Console.MarkupLine($"[red]Did you want to run the interactive command?[/]");
            return 1;
        }

        var mppgFile = new FileInfo(settings.MppgFile);
        if (!mppgFile.Exists)
        {
            Console.MarkupLine($"[red]{mppgFile.FullName}: file not found.[/]");
            return 1;
        }

        // in interactive mode, the default is not to echo each command
        var echoInput = settings.EchoInput ?? true;

        var plotsRoot = ExportRoot.ForRun(settings.PlotsRoot, settings.PlotsRootMode, mppgFile.Directory?.FullName);
        if (plotsRoot.Validate() is { } plotsRootError)
        {
            Console.MarkupLineInterpolated($"[red]{plotsRootError}[/]");
            return 1;
        }

        if(!settings.Deterministic)
            Console.MarkupLineInterpolated($"[yellow]Plots with the out option will be saved in: {plotsRoot}[/]");

        var parsingStopwatch = Stopwatch.StartNew();
        var programText = File.ReadAllText(mppgFile.FullName, Encoding.UTF8);
        var program = MppgParser.Program.FromText(programText);
        parsingStopwatch.Stop();

        if (settings.Verbose)
            Console.MarkupLine($"[gray]Parsing completed in {parsingStopwatch.Elapsed.TotalMilliseconds} ms.[/]");

        if (program.Errors.Count > 0)
        {
            if (settings.OnErrorMode == OnErrorMode.Stop)
            {
                Console.MarkupLine("[red]ERROR! Syntax errors, run aborted:[/]");
                foreach(var error in program.Errors)
                {
                    SyntaxErrorPrinter.PrintError(Console, error, "red", settings.Verbose);
                }
                return 1;
            }
            else
            {
                Console.MarkupLine("[darkorange]WARNING! Syntax errors:[/]");
                foreach(var error in program.Errors)
                {
                    SyntaxErrorPrinter.PrintError(Console, error, "darkorange", settings.Verbose);
                }
            }
        }

        var plotFormatter = settings.Deterministic ? null : 
            new ScottPlotFormatter(plotsRoot)
            {
                Console = Console,
                AutoOpenPlots = !settings.NoGui
            };
        // add option to use XPlotPlotFormatter?

        // TikZ plots are code, not images: their output is deterministic, hence they are not disabled by --deterministic
        var tikzPlotFormatter = new TikzPlotFormatter(plotsRoot)
        {
            Console = Console
        };

        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.ConvertReferencePrints => new OutputOnlyFormatter()
            {
                Console = Console,
                PlotFormatter = plotFormatter,
                TikzPlotFormatter = tikzPlotFormatter,
            },
            OutputMode.MppgClassic => new MppgClassicStatementFormatter { Console = Console, Verbose = settings.Verbose },
            OutputMode.NancyNew => new AnsiConsoleStatementFormatter()
            {
                Console = Console,
                PlotFormatter = plotFormatter,
                TikzPlotFormatter = tikzPlotFormatter,
                PrintTimePerStatement = !settings.Deterministic,
                PrintInputAsConfirmation = false,
                EchoInput = echoInput,
                Verbose = settings.Verbose
            },
            _ => new MppgClassicStatementFormatter { Console = Console, Verbose = settings.Verbose }
        };

        var immediateComputeValue = settings.RunMode switch
        {
            RunMode.ExpressionsBased => false,
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
