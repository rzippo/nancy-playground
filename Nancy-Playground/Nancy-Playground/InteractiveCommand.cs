using System.Text.RegularExpressions;
using NancyPlayground;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.MppgParser;
using Unipi.MppgParser.Utility;

namespace NancyMppg;

public partial class InteractiveCommand : Command<InteractiveCommand.Settings>
{
    public sealed class Settings : CommonExecutionSettings
    {
        
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var programContext = new ProgramContext();

        // todo: make this configurable
        var plotsRoot = Environment.CurrentDirectory;

        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.MppgClassic => new PlainConsoleStatementFormatter(),
            OutputMode.NancyNew => new AnsiConsoleStatementFormatter()
            {
                // todo: make this configurable
                PlotFormatter = new ScottPlotFormatter(plotsRoot)
                // PlotFormatter = new XPlotPlotFormatter(plotsRoot)
            },
            _ => new PlainConsoleStatementFormatter()
        };
        
        var immediateComputeValue = settings.RunMode switch
        {
            RunMode.ExpressionDriven => false,
            RunMode.PerStatement => true,
            _ => false
        };

        var lineEditor = new LineEditor(Keywords, ContextualKeywords());
        var totalComputationTime = TimeSpan.Zero;
        AnsiConsole.MarkupLine("[green]This is Nancy-Playground, interactive mode. Type your commands. Use [blue]!help[/] to read the manual.[/]");
        while (true)
        {
            var line = lineEditor.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                AnsiConsole.WriteLine();
            else if (line.StartsWith("!"))
            {
                // interactive mode command
                if (line.StartsWith("!quit") || line.StartsWith("!exit"))
                {
                    AnsiConsole.MarkupLine("[green]Bye.[/]");
                    break;
                }
                else if (line.StartsWith("!export"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    ExportProgram(args, programContext);
                }
                else if (line.StartsWith("!help"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    PrintHelp(args);
                }
            }
            else
            {
                // MPPG syntax statement
                var statement = Statement.FromLine(line);
                programContext.ExecuteStatement(statement, formatter, immediateComputeValue);
            }
        }

        return 0;
    }

    private void ExportProgram(string[] args, ProgramContext programContext)
    {
        if (args.Length != 1)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] !export requires exactly one argument: the output file path.");
            return;
        }

        var outputPath = args[0];
        try
        {
            var statementLines = programContext.StatementHistory
                .Select(s => s.Text);
            System.IO.File.WriteAllLines(outputPath, statementLines);
            AnsiConsole.MarkupLine($"[green]Program exported successfully to[/] [blue]{Escape(outputPath)}[/].");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not export program to [blue]{Escape(outputPath)}[/]: {Escape(e.Message)}");
        }
    }
}