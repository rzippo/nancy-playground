using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.Cli;

public class ConvertCommand : Command<ConvertCommand.Settings>
{
    public sealed class Settings : CommonExecutionSettings
    {
        [Description("Path to the .mppg file to convert to a Nancy program.")]
        [CommandArgument(0, "<file>")]
        public string MppgFile { get; init; } = string.Empty;
        
        [Description("Path to the .mppg file to convert to a Nancy program.")]
        [CommandOption("--output-file")]
        public string NancyCsFile { get; init; } = string.Empty;

        [Description("If true, the Nancy program will use Nancy.Expressions syntax.")]
        [CommandOption("--use-expressions")]
        public bool UseNancyExpressions { get; init; } = false;
        
        [Description("If true, the Nancy program will be overwritten if already exists.")]
        [CommandOption("--overwrite")]
        public bool Overwrite { get; init; } = false;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
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
        
        var nancyFilePath = settings.NancyCsFile.IsNullOrWhiteSpace() ?
            Path.Join(mppgFile.Directory!.FullName, $"{mppgFile.Name}.cs") :
            settings.NancyCsFile;
        var nancyFile = new FileInfo(nancyFilePath);
        if (nancyFile.Exists && !settings.Overwrite)
        {
            AnsiConsole.MarkupLine($"[red]{nancyFile.FullName}: file already exists.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[yellow]Output program will be saved in: {nancyFile.FullName}[/]");

        var programText = File.ReadAllText(mppgFile.FullName);
        var code = MppgParser.Program.ToNancyCode(programText);
        code.InsertRange(0,[
            $"// Program automatically converted from MPPG syntax to a Nancy program",
            $"// Original source was in {mppgFile.FullName}",
            string.Empty
        ]);
        
        File.WriteAllLines(nancyFile.FullName, (IEnumerable<string>)code);
        
        AnsiConsole.MarkupLine($"[yellow]Conversion complete.[/]");
        
        return 0;
    }
}