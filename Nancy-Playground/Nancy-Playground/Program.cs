
using System.Globalization;
using NancyMppg;
using Spectre.Console.Cli;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var app = new CommandApp<InteractiveCommand>();
app.Configure(config =>
{
    config.AddCommand<RunCommand>("run");
    config.AddCommand<InteractiveCommand>("interactive");
    config.AddCommand<SetupCommand>("setup");
});
return app.Run(args);