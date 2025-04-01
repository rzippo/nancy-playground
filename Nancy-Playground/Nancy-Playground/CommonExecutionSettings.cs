using Spectre.Console.Cli;

namespace NancyMppg;

public class CommonExecutionSettings : CommandSettings
{
    [CommandOption("-o|--output-mode")]
    public OutputMode? OutputMode { get; init; }
    
    [CommandOption("-r|--run-mode")]
    public RunMode? RunMode { get; init; }
    
    [CommandOption("-e|--on-error")]
    public OnErrorMode? OnErrorMode { get; init; }
}

public enum OutputMode
{
    /// <summary>
    /// Follows the output style of RTaW Min-Plus Playground.
    /// </summary>
    MppgClassic,
    /// <summary>
    /// Uses a richer custom output style.
    /// </summary>
    NancyNew
}

public enum RunMode
{
    /// <summary>
    /// Each statement trigger its related computation.
    /// </summary>
    PerStatement,
    /// <summary>
    /// Statements build up expressions, which are lazily evaluated only when required.
    /// </summary>
    ExpressionDriven
}

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