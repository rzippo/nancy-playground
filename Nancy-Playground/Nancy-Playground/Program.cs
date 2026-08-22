using System.Globalization;
using System.Reflection;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// The entry point of the CLI, and what it reports about the build it comes from.
/// </summary>
public class Program
{
    /// <summary>
    /// The version of the build.
    /// </summary>
    public static string Version => Assembly
        .GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "PackageVersion")?.Value
        ?? "NA";

    /// <summary>
    /// The commit the build was made from.
    /// </summary>
    public static string GitCommit => Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "GitCommit")?.Value
        ?? "NA";

    /// <summary>
    /// The commit the build was made from, shortened for display.
    /// </summary>
    public static string GitCommitShort => 
        GitCommit.Length >= 7 ? GitCommit[..7] : GitCommit;

    /// <summary>
    /// The one line naming the tool and its version.
    /// </summary>
    public static string CliVersionLine =>
        $"[green]This is [blue]nancy-playground[/], version {Version} ({GitCommitShort}).[/]";

    /// <summary>
    /// The lines shown when a session starts.
    /// </summary>
    public static List<string> CliWelcomeMessage =>
    [
        CliVersionLine,
        // todo: add reference to the maintainer somewhere?
        "[green]Academic attribution: if useful, please cite [yellow]https://doi.org/10.4230/LIPIcs.ECRTS.2026.5[/][/]"
    ];

    /// <summary>
    /// Runs the command <paramref name="args"/> asks for, returning its exit code.
    /// </summary>
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (Console.IsOutputRedirected)
        {
            // Encoding must be set first, since AnsiConsole captures Console.Out on first use
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AnsiConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AnsiConsole.Profile.Capabilities.Ansi = false;
            AnsiConsole.Profile.Width = int.MaxValue;
        }
        if (Console.IsInputRedirected)
        {
            // the line editor reads keys, which requires a terminal
            AnsiConsole.Profile.Capabilities.Interactive = false;
        }

        var app = BuildNancyPlaygroundApp();
        return app.Run(args);
    }

    /// <summary>
    /// Builds the command line, i.e. the commands and the options each accepts.
    /// </summary>
    public static CommandApp<InteractiveCommand> BuildNancyPlaygroundApp()
    {
        var app = new CommandApp<InteractiveCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("nancy-playground");
            config.AddCommand<RunCommand>("run")
                .WithDescription("Runs a .mppg script")
                .WithExample("run", "./Examples/hal-04513292v1.mppg")
                .WithExample("run", "./Examples/hal-04513292v1.mppg", "--output-mode", "MppgClassic", "--run-mode", "PerStatement")
                .WithExample("run", "./Examples/hal-04513292v1.mppg", "--output-mode", "NancyNew", "--run-mode", "ExpressionsBased");

            config.AddCommand<InteractiveCommand>("interactive")
                .WithDescription("Interactive mode, where the user can input MPPG lines one by one.");

            config.AddCommand<ConvertCommand>("convert")
                .WithDescription("Converts a .mppg file to a Nancy program");

            config.AddCommand<ManualCommand>("manual")
                .WithDescription("Shows the MPPG syntax manual and exits. Optionally filter by a search query.");

#if USE_PLAYWRIGHT
            config.AddCommand<SetupCommand>("setup")
                .WithDescription("Initializes dependencies. Required to enable exporting plots to images.");
#endif

#if DEBUG
            config.PropagateExceptions();
#endif
        });
        return app;
    }
}