using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

public class Program
{
    public static List<string> CliWelcomeMessage =
    [
        "[green]This is [blue]nancy-playground[/], version 1.0.0.[/]",
        // todo: add reference to the maintainer somewhere?
        "[green]Academic attribution: if useful, please cite [yellow]https://doi.org/10.1016/j.softx.2022.101178[/][/]"
    ];
    
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (Console.IsOutputRedirected)
        {
            AnsiConsole.Profile.Capabilities.Ansi = false;
            AnsiConsole.Profile.Width = int.MaxValue;
        }

        var app = new CommandApp<InteractiveCommand>();
        app.Configure(config =>
        {
            config.AddCommand<RunCommand>("run")
                .WithDescription("Runs a .mppg script")
                .WithExample("run", "./Examples/hal-04513292v1.mppg")
                .WithExample("run", "./Examples/hal-04513292v1.mppg", "--output-mode", "MppgClassic", "--run-mode", "PerStatement")
                .WithExample("run", "./Examples/hal-04513292v1.mppg", "--output-mode", "NancyNew", "--run-mode", "ExpressionsBased");
    
            config.AddCommand<InteractiveCommand>("interactive")
                .WithDescription("Interactive mode, where the user can input MPPG lines one by one.");

            config.AddCommand<ConvertCommand>("convert");
    
#if USE_PLAYWRIGHT
            config.AddCommand<SetupCommand>("setup")
                .WithDescription("Initializes dependencies. Required to enable exporting plots to images.");
#endif

#if DEBUG
            config.PropagateExceptions();
#endif
        });
        return app.Run(args);
    }
}