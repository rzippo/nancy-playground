using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

[ExcludeFromCodeCoverage]
public class ManualCommand : Command<ManualCommand.Settings>
{
    private IAnsiConsole Console { get; } = AnsiConsole.Console;

    public ManualCommand() { }

    public ManualCommand(IAnsiConsole console)
    {
        Console = console;
    }

    public sealed class Settings : CommonExecutionSettings
    {
        [Description("Optional search query to filter the manual by keyword.")]
        [CommandArgument(0, "[query]")]
        public string? Query { get; init; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Version)
        {
            Console.MarkupLine(Program.CliVersionLine);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.Query))
        {
            InteractiveCommand.PrintSearchLong(
                Console,
                NancyPlaygroundDocs.HelpDocument,
                [settings.Query]
            );
        }
        else
        {
            InteractiveCommand.PrintShort(Console, NancyPlaygroundDocs.HelpDocument);
        }

        return 0;
    }
}
