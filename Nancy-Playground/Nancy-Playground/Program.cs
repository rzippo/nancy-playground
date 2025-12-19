
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;
using Unipi.Nancy.Playground.Cli;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// CLI welcome message
AnsiConsole.MarkupLine("[green]This is [blue]nancy-playground[/], version 1.0.0.[/]");
// todo: add reference to the maintainer somewhere?
AnsiConsole.MarkupLine("[green]Academic attribution: if useful, please cite [yellow]https://doi.org/10.1016/j.softx.2022.101178[/][/]");

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
    
    config.AddCommand<SetupCommand>("setup")
        .WithDescription("Initializes dependencies. Required to enable exporting plots to images.");

    #if DEBUG
    config.PropagateExceptions();
    #endif
});
return app.Run(args);