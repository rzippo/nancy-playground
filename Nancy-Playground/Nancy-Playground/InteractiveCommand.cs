using System.Text.RegularExpressions;
using NancyPlayground;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.MppgParser;
using Unipi.MppgParser.Utility;

namespace NancyMppg;

public class InteractiveCommand : Command<InteractiveCommand.Settings>
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

    private void PrintHelp(string[] args)
    {
        if (args.Length > 0)
            PrintSearchLong(NancyPlaygroundDocs.HelpDocument, args);
        else
            PrintShort(NancyPlaygroundDocs.HelpDocument);
    }
    
    /// <summary>
    /// Prints all sections and items of a HelpDocument in a short, colored form.
    /// </summary>
    public static void PrintShort(HelpDocument doc)
    {
        if (!string.IsNullOrWhiteSpace(doc.Preamble))
        {
            AnsiConsole.MarkupLine($"[grey]{Escape(doc.Preamble.Trim())}[/]");
            AnsiConsole.WriteLine();
        }

        foreach (var section in doc.Sections)
        {
            PrintSectionShort(section);
            AnsiConsole.WriteLine();
        }
    }

    public static void PrintSearchLong(HelpDocument doc, IReadOnlyList<string> args)
    {
        var searchMatches = NancyPlaygroundDocs.HelpDocument.Sections
            .Where(section =>
                section.Tags.Any(tag => Regex.IsMatch(tag, args[0])) ||
                section.Items.Any(item =>
                    item.Tags.Any(tag => Regex.IsMatch(tag, args[0]))
                )
            )
            .Select(section =>
                {
                    if (section.Tags.Any(tag => Regex.IsMatch(tag, args[0])))
                        return section;
                    else
                    {
                        var filteredSection = section with
                        {
                            Items = section.Items
                                .Where(item => item.Tags.Any(tag => Regex.IsMatch(tag, args[0])))
                                .ToList()
                        };
                        return filteredSection;
                    }
                }    
            )
            .ToList();

        if (searchMatches.Any())
        {
            // if (!string.IsNullOrWhiteSpace(doc.Preamble))
            // {
            //     AnsiConsole.MarkupLine($"[grey]{Escape(doc.Preamble.Trim())}[/]");
            //     AnsiConsole.WriteLine();
            // }
            foreach (var section in searchMatches)
                PrintSectionLong(section);
        }
        else
            AnsiConsole.MarkupLine($"[yellow]No match found for the given keywords.[/]");
    }

    private static void PrintSectionShort(HelpSection section)
    {
        var tagText = section.Tags is { Count: > 0 }
            ? $" [grey]({Escape(string.Join(", ", section.Tags))})[/]"
            : string.Empty;

        // AnsiConsole.MarkupLine($"[bold yellow]{Escape(section.Name)}[/]{tagText}");
        AnsiConsole.MarkupLine($"[bold yellow]{Escape(section.Name)}[/]");

        if (!string.IsNullOrWhiteSpace(section.Description))
        {
            AnsiConsole.MarkupLine($"[dim]{Escape(section.Description)}[/]");
        }

        foreach (var item in section.Items)
        {
            PrintItemShort(item);
        }
    }

    private static void PrintItemShort(HelpItem item)
    {
        var tagText = item.Tags is { Count: > 0 }
            ? $" [grey]({Escape(string.Join(", ", item.Tags))})[/]"
            : string.Empty;

        // Item name + optional tags
        // AnsiConsole.MarkupLine($"  [cyan]- {Escape(item.Name)}[/]{tagText}");
        AnsiConsole.MarkupLine($"  [cyan]- {Escape(item.Name)}[/] [green]{Escape(item.Format)}[/]");

        // One-line short description (truncated)
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            // var shortDesc = TruncateSingleLine(item.Description, 80);
            // AnsiConsole.MarkupLine($"    [dim]{Escape(shortDesc)}[/]");
            AnsiConsole.MarkupLine($"    [dim]{Escape(item.Description)}[/]");
        }
    }
    
    private static void PrintSectionLong(HelpSection section)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{Escape(section.Name)}[/]");

        var tagText = section.Tags is { Count: > 0 }
            ? $" [grey]({Escape(string.Join(", ", section.Tags))})[/]"
            : string.Empty;
        AnsiConsole.MarkupLine(tagText);

        if (!string.IsNullOrWhiteSpace(section.Description))
            AnsiConsole.MarkupLine($"[dim]{Escape(section.Description)}[/]");
        
        foreach (var item in section.Items)
            PrintItemLong(item);
    }
    
    private static void PrintItemLong(HelpItem item)
    {
        AnsiConsole.MarkupLine($"  [cyan]- {Escape(item.Name)}[/] [green]{Escape(item.Format)}[/]");
        
        var tagText = item.Tags is { Count: > 0 }
            ? $" [grey]({Escape(string.Join(", ", item.Tags))})[/]"
            : string.Empty;
        AnsiConsole.MarkupLine(tagText);

        var description = item.LongDescription.IsNullOrWhiteSpace() ?
                item.Description :
                item.LongDescription;
        var descriptionLines = description.Split("\n");
        foreach (var descriptionLine in descriptionLines)
            AnsiConsole.MarkupLine($"    [dim]{Escape(descriptionLine)}[/]");
    }

    private static string TruncateSingleLine(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var oneLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (oneLine.Length <= maxLength)
            return oneLine;

        return oneLine[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Escapes Spectre.Console markup special characters.
    /// </summary>
    private static string Escape(string text)
    {
        return Markup.Escape(text ?? string.Empty);
    }

    private static List<string> Keywords = [
        // higher-order commands
        "!help",
        "!quit",
        "!exit",
        "!clear",
        // curves
        "ratency",
        "bucket",
        "affine",
        "step",
        "stair",
        "delay",
        "zero",
        "epsilon",
        "upp",
        "uaf",
        // operations
        "star",
        "hShift",
        "vShift",
        "inv",
        "low_inv",
        "up_inv",
        "upclosure",
        "nnupclosure",
        "comp",
        "left-ext",
        "right-ext",
        "hDev",
        "vDev",
        "zDev",
        // "maxBacklogPeriod", not implemented yet
        "plot",
        "assert",
        "printExpression"
    ];

    private static List<ContextualKeywords> ContextualKeywords() => [
        new ContextualKeywords
        {
            Enablers = [ "upp" ],
            Keywords = [
                "period",
            ]
        },
        new ContextualKeywords
        {
            Enablers = [ "plot" ],
            Keywords = [
                "main",
                "title",
                "xlim",
                "ylim",
                "xlab",
                "ylab",
                "out",
                "grid",
                "bg",
                "gui",
            ]
        },
        new ContextualKeywords
        {
            Enablers = [ "!help" ],
            Keywords = NancyPlaygroundDocs.HelpDocument
                .Sections
                .SelectMany(section => section.Tags)
                .Concat(
                    NancyPlaygroundDocs.HelpDocument
                        .Sections
                        .SelectMany(section => section.Items)
                        .SelectMany(item => item.Tags)
                )
                .Distinct()
                .ToList()
        }
    ];
}