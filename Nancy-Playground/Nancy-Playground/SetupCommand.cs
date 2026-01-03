using Spectre.Console.Cli;
using Unipi.Nancy.Playground.Cli.Nancy.Plots;

namespace Unipi.Nancy.Playground.Cli;

public class SetupCommand : Command<SetupCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {

    }
    public override int Execute(CommandContext context, Settings settings)
    {
        HtmlToImage.InstallBrowser();
        return 0;
    }
}