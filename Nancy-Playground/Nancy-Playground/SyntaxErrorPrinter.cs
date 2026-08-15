using Spectre.Console;
using Unipi.Nancy.Playground.MppgParser;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Renders a <see cref="SyntaxErrorInfo"/> in the error color of the CLI: the offending line with the
/// offending token highlighted, and a caret underneath.
/// </summary>
internal static class SyntaxErrorPrinter
{
    public static void PrintError(IAnsiConsole console, SyntaxErrorInfo error, string color)
    {
        console.MarkupLineInterpolated($"[{color}]\t - line {error.Line}:{error.Column} {error.Message}[/]");
        if (error.Hint is not null)
            console.MarkupLineInterpolated($"[{color}]\t   {error.Hint}[/]");
        PrintSourceExcerpt(console, error, color);
    }

    public static void PrintSourceExcerpt(IAnsiConsole console, SyntaxErrorInfo error, string color)
    {
        if (error.SourceLine is null)
            return;

        const string indent = "\t   ";
        var column = Math.Clamp(error.Column, 0, error.SourceLine.Length);
        var length = Math.Clamp(error.OffendingText?.Length ?? 1, 0, error.SourceLine.Length - column);

        // The line before only helps when the error is at the start of its line, where it explains
        // the newline that appears in ANTLR's "no viable alternative at input '...'" span.
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
