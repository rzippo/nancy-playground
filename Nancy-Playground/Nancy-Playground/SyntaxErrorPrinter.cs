using Spectre.Console;
using Unipi.Nancy.Playground.MppgParser;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Renders a <see cref="SyntaxErrorInfo"/> in the error color of the CLI: the offending line with the offending token highlighted, and a caret underneath.
/// </summary>
internal static class SyntaxErrorPrinter
{
    /// <param name="console">Where to write.</param>
    /// <param name="error">The error to render.</param>
    /// <param name="color">The colour of the message and of the caret.</param>
    /// <param name="verbose">
    /// True for verbose output, meant for debugging.
    /// </param>
    public static void PrintError(IAnsiConsole console, SyntaxErrorInfo error, string color, bool verbose = false)
    {
        console.MarkupLineInterpolated($"[{color}]\t - line {error.Line}:{error.Column} {error.Message}[/]");
        if (error.Hint is not null)
            console.MarkupLineInterpolated($"[{color}]\t   {error.Hint}[/]");
        PrintSourceExcerpt(console, error, color);
        if (verbose)
            PrintDiagnostics(console, error);
    }

    /// <summary>
    /// What the parser said, and the matcher that reworded it, for a message that has to be traced back to what produced it.
    /// </summary>
    private static void PrintDiagnostics(IAnsiConsole console, SyntaxErrorInfo error)
    {
        const string indent = "\t   ";
        if (error.AntlrMessage is { } antlr)
            console.MarkupLineInterpolated($"[grey]{indent}parser: {antlr}[/]");
        if (error.RewrittenBy is { } matcher)
            console.MarkupLineInterpolated($"[grey]{indent}reworded by: {matcher}[/]");
        if (error.RuleName is { } rule)
            console.MarkupLineInterpolated($"[grey]{indent}rule: {rule}[/]");
    }

    public static void PrintSourceExcerpt(IAnsiConsole console, SyntaxErrorInfo error, string color)
    {
        if (error.SourceLine is null)
            return;

        const string indent = "\t   ";
        var column = Math.Clamp(error.Column, 0, error.SourceLine.Length);
        var length = Math.Clamp(error.OffendingText?.Length ?? 1, 0, error.SourceLine.Length - column);

        // The line before only helps when the error is at the start of its line, where it explains the newline that appears in ANTLR's "no viable alternative at input '...'" span.
        if (error.PreviousLine is not null && error.Column == 0)
            console.MarkupLine($"[gray]{indent}{EscapeMarkup(error.PreviousLine)}[/]");

        var before = EscapeMarkup(error.SourceLine[..column]);
        var offending = EscapeMarkup(error.SourceLine.Substring(column, length));
        var after = EscapeMarkup(error.SourceLine[(column + length)..]);

        console.MarkupLine($"[gray]{indent}{before}[/][{color}]{offending}[/][gray]{after}[/]");

        var caret = new string(' ', column) + new string('^', Math.Max(1, length));
        console.MarkupLine($"[{color}]{indent}{caret}[/]");
    }

    private static string EscapeMarkup(string text) =>
        text.Replace("[", "[[").Replace("]", "]]");
}
