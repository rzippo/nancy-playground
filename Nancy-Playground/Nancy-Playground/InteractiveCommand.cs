using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.MppgParser;

namespace NancyMppg;

public class InteractiveCommand : Command<InteractiveCommand.Settings>
{
    public sealed class Settings : CommonExecutionSettings
    {
        
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var programContext = new ProgramContext();
        
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

        var totalComputationTime = TimeSpan.Zero;
        AnsiConsole.MarkupLine("[green]This is Nancy-Playground, interactive mode. Type your commands.[/]");
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                // dumb termination
                AnsiConsole.MarkupLine("[green]Bye.[/]");
                break;
            }
            var statement = Statement.FromLine(line);
            programContext.ExecuteStatement(statement, formatter, immediateComputeValue);
        }

        return 0;
    }
}