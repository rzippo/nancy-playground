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
                else if (line.StartsWith("!convert"))
                {
                    var args = line.Split(' ').Skip(1).ToArray();
                    ConvertProgram(args, programContext);
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

    /// <summary>
    /// Exports the current program to a file.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="programContext"></param>
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

            File.WriteAllLines(outputPath, statementLines);
            AnsiConsole.MarkupLine($"[green]Program exported successfully to[/] [blue]{Escape(outputPath)}[/].");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not export program to [blue]{Escape(outputPath)}[/]: {Escape(e.Message)}");
        }
    }

    /// <summary>
    /// Converts the current MPPG program to Nancy code and writes it to a file.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="programContext"></param>
    private void ConvertProgram(string[] args, ProgramContext programContext)
    {
        if (args.Length != 1)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] !convert requires exactly one argument: the output file path.");
            return;
        }

        var outputPath = args[0];
        try
        {
            var statementLines = programContext.StatementHistory
                .Select(s => s.Text);
            var programText = string.Join(Environment.NewLine, statementLines);
            var programNancyCode = Unipi.MppgParser.Program.ToNancyCode(programText);
            programNancyCode.InsertRange(0,[
                $"// Program automatically converted from MPPG syntax to a Nancy program",
                string.Empty
            ]);

            File.WriteAllLines(outputPath, programNancyCode);
            AnsiConsole.MarkupLine($"[green]Program converted successfully to[/] [blue]{Escape(outputPath)}[/].");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not export program to [blue]{Escape(outputPath)}[/]: {Escape(e.Message)}");
        }
    }
}