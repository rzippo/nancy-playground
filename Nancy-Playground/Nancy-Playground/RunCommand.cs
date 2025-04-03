using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.MppgParser;
using Unipi.MppgParser.Grammar;

namespace NancyMppg;

public class RunCommand : Command<RunCommand.Settings>
{
    public sealed class Settings : CommonExecutionSettings
    {
        [Description("Path to the .mppg file to run")]
        [CommandArgument(0, "<file>")]
        public string MppgFile { get; init; } = string.Empty;
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
        
        var programText = File.ReadAllText(mppgFile.FullName);
        var program = Unipi.MppgParser.Program.FromText(programText);
        IStatementFormatter formatter = settings.OutputMode switch
        {
            OutputMode.MppgClassic => new PlainConsoleStatementFormatter(),
            OutputMode.NancyNew => new AnsiConsoleStatementFormatter()
            {
                PlotFormatter = new HtmlPlotFormatter()
            },
            _ => new PlainConsoleStatementFormatter()
        };

        var immediateComputeValue = settings.RunMode switch
        {
            RunMode.ExpressionDriven => false,
            RunMode.PerStatement => true,
            _ => false
        };
            
        while (!program.IsEndOfProgram)
        {
            var output = program.ExecuteNextStatement(formatter, immediateComputeValue);
            if (settings.OnErrorMode == OnErrorMode.Stop &&
                output is ErrorOutput)
                break;
        }

        return 0;
    }
}