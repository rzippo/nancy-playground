
using System.Globalization;
using NancyMppg;
using Spectre.Console.Cli;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<RunCommand>("run");
    config.AddCommand<InteractiveCommand>("interactive");
});
return app.Run(args);