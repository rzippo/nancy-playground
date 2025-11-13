
using System.Globalization;
using NancyMppg;
using Spectre.Console.Cli;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
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
    config.AddCommand<SetupCommand>("setup")
        .WithDescription("Initializes dependencies. Required to enable exporting plots to images.");

    #if DEBUG
    config.PropagateExceptions();
    #endif
});
return app.Run(args);