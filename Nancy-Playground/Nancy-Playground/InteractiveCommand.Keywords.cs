namespace Unipi.Nancy.Playground.Cli;

public partial class InteractiveCommand
{
    private static List<string> Keywords =
    [
        // higher-order commands
        "!help",
        "!manual",
        "!clihelp",
        "!quit",
        "!exit",
        "!export",
        "!save",
        "!convert",
        "!load",
        "!clear",
        // curves
        "ratency",
        "bucket",
        "affine",
        "step",
        "stair",
        "delay",
        "zero",
        "epsilon",
        "upp",
        "uaf",
        // operations
        "star",
        "subaddclosure",
        "superaddclosure",
        "hShift",
        "vShift",
        "inv",
        "low_inv",
        "up_inv",
        "upclosure",
        "nnupclosure",
        "lowclosure",
        "nnlowclosure",
        "floor",
        "ceil",
        "comp",
        "left-ext",
        "right-ext",
        "hDev",
        "vDev",
        "zDev",
        // scalar operations
        "abs",
        "pow",
        "mod",
        "gcd",
        "lcm",
        // "maxBacklogPeriod", not implemented yet
        "plot",
        "plotTikz",
        "assert",
        "printExpression"
    ];

    private static List<ContextualKeywords> ContextualKeywords() =>
    [
        new ContextualKeywords
        {
            Enablers = ["upp"],
            Keywords =
            [
                "period",
            ]
        },
        new ContextualKeywords
        {
            Enablers = ["plot", "plotTikz"],
            Keywords =
            [
                "main",
                "title",
                "xlim",
                "ylim",
                "xlab",
                "ylab",
                "out",
                "grid",
                "bg",
                "gui",
            ]
        },
        new ContextualKeywords
        {
            Enablers = ["!help", "!manual"],
            Keywords = NancyPlaygroundDocs.HelpDocument
                .Sections
                .SelectMany(section => section.Tags)
                .Concat(
                    NancyPlaygroundDocs.HelpDocument
                        .Sections
                        .SelectMany(section => section.Items)
                        .SelectMany(item => item.Tags)
                )
                .Distinct()
                .ToList()
        }
    ];
}