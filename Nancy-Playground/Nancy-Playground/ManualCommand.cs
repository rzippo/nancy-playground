using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// The 'manual' command, which prints the manual of the syntax.
/// </summary>
[ExcludeFromCodeCoverage]
public class ManualCommand : Command<ManualCommand.Settings>
{
    private IAnsiConsole Console { get; } = AnsiConsole.Console;

    /// <summary>
    /// A command writing to the default console.
    /// </summary>
    public ManualCommand() { }

    /// <summary>
    /// A command writing to <paramref name="console"/>.
    /// </summary>
    public ManualCommand(IAnsiConsole console)
    {
        Console = console;
    }

    /// <summary>
    /// The options of the 'manual' command.
    /// </summary>
    public sealed class Settings : CommonExecutionSettings
    {
        /// <summary>
        /// What to look up, or null to print the whole manual.
        /// </summary>
        [Description("Optional search query to filter the manual by keyword.")]
        [CommandArgument(0, "[query]")]
        public string? Query { get; init; }
    }

    /// <summary>
    /// Prints the manual, returning the exit code of the command.
    /// </summary>
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
