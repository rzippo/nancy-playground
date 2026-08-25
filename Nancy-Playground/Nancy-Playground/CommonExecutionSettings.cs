using System.ComponentModel;
using Spectre.Console.Cli;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// The options every command of the CLI accepts.
/// </summary>
public class CommonExecutionSettings : CommandSettings
{
    /// <summary>
    /// How the output is formatted.
    /// </summary>
    [CommandOption("-o|--output-mode")] 
    [Description("How the output is formatted. Available options: ConvertReferencePrints, MppgClassic, NancyNew (default).")]
    public OutputMode? OutputMode { get; init; } 
        = Cli.OutputMode.NancyNew;
    
    /// <summary>
    /// When the computations are performed, i.e. at each statement or only where a value is needed.
    /// </summary>
    [CommandOption("-r|--run-mode")]
    [Description("How the computations are performed. Available options are PerStatement (computes the result of each line as it comes), ExpressionsBased (computes only as needed, e.g. for plots and value prints). Default: ExpressionsBased.")]
    public RunMode? RunMode { get; init; }
        = Cli.RunMode.ExpressionsBased;
    
    /// <summary>
    /// What to do when a statement fails.
    /// </summary>
    [CommandOption("-e|--on-error")]
    [Description("Specifies what to do when an error occurs. Available options: Stop (default), Continue.")]
    public OnErrorMode? OnErrorMode { get; init; }
        = Cli.OnErrorMode.Stop;

    /// <summary>
    /// True to mute the welcome message.
    /// </summary>
    [CommandOption("--no-welcome")]
    [Description("Mutes the welcome message.")]
    public bool MuteWelcomeMessage { get; init; } = false;

    /// <summary>
    /// True to never open a plot in a window, whatever the plot commands ask.
    /// The image is still written and its path printed.
    /// </summary>
    [CommandOption("--no-gui")]
    [Description("Never shows a plot in a GUI window, overriding the gui option of each plot command. The image is still written, and its path printed. Has no effect on plotTikz, which uses no GUI.")]
    public bool NoGui { get; init; } = false;

    /// <summary>
    /// True to echo each statement before its output, or null to take the default of the command.
    /// </summary>
    [CommandOption("--echo")]
    [Description("Echoes user input in interactive mode. Default: true in run mode, false in interactive mode.")]
    public bool? EchoInput { get; init; }

    /// <summary>
    /// True to read whole lines rather than use the line editor, or null to decide from whether the input is piped.
    /// </summary>
    [CommandOption("--line-input")]
    [Description("Reads whole lines instead of using the interactive line editor. Default: enabled when the input is piped.")]
    public bool? LineInput { get; init; }

    /// <summary>
    /// True to print what the run did beyond its output, e.g. how long parsing took.
    /// </summary>
    [CommandOption("--verbose")]
    [Description("If enabled, the program prints out additional information about the execution, such as the time taken during parsing. Default: false.")]
    public bool Verbose {get; init;} = false;

    /// <summary>
    /// True to print the version and stop.
    /// </summary>
    [Description("If used, the program prints out the version and immediately terminates.")]
    [CommandOption("--version")]
    public bool Version { get; init; } = false;
}

/// <summary>
/// The style the output of a statement is written in.
/// </summary>
public enum OutputMode
{
    /// <summary>
    /// Prints in the notation of Nancy, so a converted program can be compared against it.
    /// Only prints when explicitly asked with a non-assignment expression.
    /// </summary>
    ConvertReferencePrints,
    /// <summary>
    /// Follows the output style of RTaW Min-Plus Playground.
    /// </summary>
    MppgClassic,
    /// <summary>
    /// Uses a richer custom output style.
    /// </summary>
    NancyNew
}

/// <summary>
/// When the value of an expression is computed.
/// </summary>
public enum RunMode
{
    /// <summary>
    /// Each statement trigger its related computation.
    /// </summary>
    PerStatement,
    /// <summary>
    /// Statements build up expressions, which are lazily evaluated only when required.
    /// </summary>
    ExpressionsBased
}

/// <summary>
/// What a failing statement does to the run.
/// </summary>
public enum OnErrorMode
{
    /// <summary>
    /// On error, the execution stops.
    /// </summary>
    Stop,
    /// <summary>
    /// On error, continue to the next statement.
    /// This is what RTaW Min-Plus Playground does.
    /// </summary>
    Continue
}