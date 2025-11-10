using Spectre.Console.Cli;

namespace NancyMppg;

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