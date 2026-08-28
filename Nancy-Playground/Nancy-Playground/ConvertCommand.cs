using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// The <c>convert</c> command, which writes an MPPG program as the C# program that runs it with Nancy.
/// </summary>
public class ConvertCommand : Command<ConvertCommand.Settings>
{
    private IAnsiConsole Console {get; init;} = AnsiConsole.Console;
    
    /// <summary>
    /// The options of the <c>convert</c> command.
    /// </summary>
    public sealed class Settings : CommonExecutionSettings
    {
        /// <summary>
        /// The program to convert.
        /// </summary>
        [Description("Path to the .mppg file to convert to a Nancy program.")]
        [CommandArgument(0, "<file>")]
        public string MppgFile { get; init; } = string.Empty;

        /// <summary>
        /// Where to write the generated program.
        /// </summary>
        [Description("Path of the generated program. Defaults to the source path with .cs appended.")]
        [CommandOption("--output-file")]
        public string NancyCsFile { get; init; } = string.Empty;

        /// <summary>
        /// True to write the whole path of the source into the generated program, rather than its name alone.
        /// </summary>
        [Description("If true, the header of the generated program records the full path of the source, rather than its name alone. The name alone keeps the generated program shareable, as it does not disclose the local directory layout.")]
        [CommandOption("--full-source-path")]
        public bool FullSourcePath { get; init; } = false;

        /// <summary>
        /// True to generate code that builds expressions with Unipi.Nancy.Expressions, rather than the Nancy API.
        /// </summary>
        [Description("If true, the Nancy program will use Nancy.Expressions syntax.")]
        [CommandOption("--use-expressions")]
        public bool UseNancyExpressions { get; init; } = false;

        /// <summary>
        /// True to generate code through the provisional Roslyn syntax-tree generator.
        /// </summary>
        [Description("If true, the Nancy program will be generated through the provisional Roslyn syntax-tree generator.")]
        [CommandOption("--use-code-trees")]
        public bool UseCodeTrees { get; init; } = false;

        /// <summary>
        /// True to overwrite the output file where it exists.
        /// </summary>
        [Description("If true, the Nancy program will be overwritten if already exists.")]
        [CommandOption("--overwrite")]
        public bool Overwrite { get; init; } = false;
    }

    /// <summary>
    /// A command writing to <paramref name="console"/>.
    /// </summary>
    public ConvertCommand(IAnsiConsole console)
    {
        Console = console;
    }

    /// <summary>
    /// Converts the program and writes it out, returning the exit code of the command.
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

        var nancyFilePath = settings.NancyCsFile.IsNullOrWhiteSpace() ?
            Path.Join(mppgFile.Directory!.FullName, $"{mppgFile.Name}.cs") :
            settings.NancyCsFile;
        var nancyFile = new FileInfo(nancyFilePath);
        if (nancyFile.Exists && !settings.Overwrite)
        {
            Console.MarkupLine($"[red]{nancyFile.FullName}: file already exists.[/]");
            return 1;
        }

        Console.MarkupLine($"[yellow]Output program will be saved in: {nancyFile.FullName}[/]");

        var programText = File.ReadAllText(mppgFile.FullName);
        List<string> code;
        try
        {
            code = MppgParser.Program.ToNancyCode(
                programText,
                settings.UseNancyExpressions,
                settings.UseCodeTrees);
        }
        catch (SyntaxErrorException ex) when (ex.Error is { } error)
        {
            Console.MarkupLine($"[red]Error:[/] Cannot compile program from {Markup.Escape(mppgFile.FullName)}:");
            SyntaxErrorPrinter.PrintError(Console, error, "red", settings.Verbose);
            return 1;
        }
        catch (Exception ex)
        {
            Console.MarkupLine($"[red]Error:[/] Cannot compile program from {Markup.Escape(mppgFile.FullName)}: {Markup.Escape(ex.Message)}");
            return 1;
        }
        var programType = settings.UseNancyExpressions ? "Nancy.Expressions" : "Nancy";
        var source = settings.FullSourcePath ? mppgFile.FullName : mppgFile.Name;
        code.InsertRange(0,[
            $"// Program automatically converted from MPPG syntax to a {programType} program",
            $"// Converted from {source}",
            string.Empty,
            $"// This is a file-based app: to run it, use the command `dotnet run file.cs`",
            $"// To extend it, it is recommended to convert it to a C# project with the command `dotnet project convert file.cs`",
            $"// Docs: https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps",
            string.Empty
        ]);

        File.WriteAllLines(nancyFile.FullName, (IEnumerable<string>)code);

        Console.MarkupLine($"[yellow]Conversion complete.[/]");

        return 0;
    }
}
