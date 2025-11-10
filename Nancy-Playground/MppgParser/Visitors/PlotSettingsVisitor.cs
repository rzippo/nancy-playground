using Unipi.MppgParser.Grammar;
using Unipi.MppgParser.Utility;

namespace Unipi.MppgParser.Visitors;

public class PlotSettingsVisitor : MppgBaseVisitor<PlotSettings>
{
    public override PlotSettings VisitPlotArgs(Grammar.MppgParser.PlotArgsContext context)
    {
        var plotArgContexts = context.GetRuleContexts<Grammar.MppgParser.PlotArgContext>();
        var settings = new PlotSettings();

        foreach (var plotArgContext in plotArgContexts)
        {
            var argName = plotArgContext.GetChild(0).GetText();
            var argString = plotArgContext.GetChild(2).GetText()
                .TrimQuotes();

            switch (argName)
            {
                case "main":
                {
                    // todo
                    break;
                }
                
                case "xlim":
                {
                    // todo
                    break;
                }
                
                case "ylim":
                {
                    // todo
                    break;
                }
                
                case "xlab":
                {
                    // todo
                    break;
                }
                
                case "ylab":
                {
                    // todo
                    break;
                }
                
                case "out":
                {
                    settings = settings with
                    {
                        OutPath = argString
                    };
                    break;
                }
                
                case "grid":
                {
                    settings = settings with
                    {
                        ShowGrid = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                case "bg":
                {
                    settings = settings with
                    {
                        ShowBackground = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                case "browser":
                {
                    settings = settings with
                    {
                        ShowInBrowser = argString switch
                        {
                            "yes" => true,
                            "no" => false,
                            _ => true
                        }
                    };
                    break;
                }
                
                default:
                    // do nothing
                    break;
            }
        }
        return settings;
    }
}