using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Unipi.Nancy.Playground.MppgParser;

public sealed record SyntaxErrorInfo(
    int Line,
    int Column,
    string Message,

    SyntaxErrorInfo.ErrorType Type,
    
    // Offending token / character
    string? OffendingText,
    int? OffendingTokenType,

    // Parser context (null for lexer errors)
    string? RuleName,
    IReadOnlyList<string>? RuleStack,

    // Expected tokens (parser only)
    IReadOnlyList<string>? Expected,

    // The source line the offending token is on, and the line before it, raw and unescaped.
    // Presentation, such as color or a caret, is up to the display layer.
    string? SourceLine,
    string? PreviousLine,

    // What likely caused the error, when it can be told apart from the message alone
    string? Hint = null
)
{
    public enum ErrorType { Lexer, Parser}

    public override string ToString()
        => ToString(false);
    public string ToString(bool verbose)
    {
        if (verbose)
        {
            return $"line {Line}:{Column} {Message}" +
                   (RuleName != null ? $" [rule: {RuleName}]" : "") +
                   (Expected != null && Expected.Count > 0
                       ? $" expected: {string.Join(", ", Expected)}"
                       : "") +
                   (Hint != null ? $" {Hint}" : "");
        }
        else
        {
            return $"line {Line}:{Column} {Message}" + (Hint != null ? $" {Hint}" : "");
        }
    }
};

/// <summary>
/// Extracts the offending line, and the line before it, from the character stream of an error.
/// Pure data: the lines are returned raw, with no escaping or decoration.
/// </summary>
internal static class SourceLineExtractor
{
    public static string? GetText(ICharStream? input) =>
        input is { Size: > 0 } ? input.GetText(Interval.Of(0, input.Size - 1)) : null;

    /// <summary>
    /// The source line containing <paramref name="charIndex"/>, and the line before it, or null if
    /// the index is out of range.
    /// </summary>
    public static (string Line, string? Previous)? ExtractLines(string text, int charIndex)
    {
        if (string.IsNullOrEmpty(text) || charIndex < 0 || charIndex >= text.Length)
            return null;

        var lineStart = text.LastIndexOf('\n', Math.Max(0, charIndex - 1)) + 1;
        var lineEnd = text.IndexOf('\n', charIndex);
        if (lineEnd < 0)
            lineEnd = text.Length;

        var line = text[lineStart..lineEnd].TrimEnd('\r');

        string? previous = null;
        if (lineStart > 0)
        {
            var prevEnd = lineStart - 1;
            var prevStart = text.LastIndexOf('\n', Math.Max(0, prevEnd - 1)) + 1;
            previous = text[prevStart..prevEnd].TrimEnd('\r');
        }

        return (line, previous);
    }

    /// <summary>
    /// The absolute character index of a 1-based line and 0-based column, or null if past the end.
    /// </summary>
    public static int? CharIndexOfLine(string text, int line1Based, int col0Based)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var targetLine = Math.Max(1, line1Based);
        var line = 1;
        var i = 0;
        while (i < text.Length && line < targetLine)
        {
            if (text[i] == '\n')
                line++;
            i++;
        }

        var index = i + col0Based;
        return index < text.Length ? index : text.Length - 1;
    }
}

public sealed class DiagnosticLexerErrorListener : IAntlrErrorListener<int>
{
    private readonly IList<SyntaxErrorInfo> _errors;
    private readonly ICharStream? _charStream;

    public DiagnosticLexerErrorListener(IList<SyntaxErrorInfo> errors, ICharStream? charStream)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _charStream = charStream;
    }
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        string? offendingText = null;
        if (offendingSymbol >= 0)
        {
            // offendingSymbol is a Unicode code point
            offendingText = char.ConvertFromUtf32(offendingSymbol);
        }

        var text = SourceLineExtractor.GetText(_charStream);
        var charIndex = text is null
            ? null
            : SourceLineExtractor.CharIndexOfLine(text, line, charPositionInLine);
        var lines = text is not null && charIndex is int i
            ? SourceLineExtractor.ExtractLines(text, i)
            : null;

        _errors.Add(new SyntaxErrorInfo(
            Line: line,
            Column: charPositionInLine,
            Message: msg,
            Type: SyntaxErrorInfo.ErrorType.Lexer,
            OffendingText: offendingText,
            OffendingTokenType: null,
            RuleName: null,
            RuleStack: null,
            Expected: null,
            SourceLine: lines?.Line,
            PreviousLine: lines?.Previous
        ));
    }
}

public sealed class DiagnosticParserErrorListener : BaseErrorListener
{
    private readonly IList<SyntaxErrorInfo> _errors;


    public DiagnosticParserErrorListener(IList<SyntaxErrorInfo> errors)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        string? ruleName = null;
        IReadOnlyList<string>? ruleStack = null;
        IReadOnlyList<string>? expected = null;
        string? hint = null;

        if (recognizer is Parser parser)
        {
            var ctx = parser.Context;
            if (ctx != null)
            {
                ruleName = SafeRuleName(parser, ctx.RuleIndex);
                ruleStack = GetRuleStack(parser, ctx);
            }

            expected = GetExpectedTokenNames(parser);
            hint = VersionedKeywords.TryGetUsedAsNameHint(
                VersionedKeywords.TokensOfLine(parser.TokenStream, offendingSymbol));
        }

        var text = SourceLineExtractor.GetText(offendingSymbol?.TokenSource?.InputStream);
        var lines = text is not null && offendingSymbol?.StartIndex is int start and >= 0
            ? SourceLineExtractor.ExtractLines(text, start)
            : null;

        _errors.Add(new SyntaxErrorInfo(
            Line: line,
            Column: charPositionInLine,
            Message: msg,
            Type: SyntaxErrorInfo.ErrorType.Parser,
            OffendingText: offendingSymbol?.Text,
            OffendingTokenType: offendingSymbol?.Type,
            RuleName: ruleName,
            RuleStack: ruleStack,
            Expected: expected,
            SourceLine: lines?.Line,
            PreviousLine: lines?.Previous,
            Hint: hint
        ));
    }

    private static string? SafeRuleName(Parser parser, int ruleIndex)
    {
        if (parser.RuleNames == null) return null;
        return (ruleIndex >= 0 && ruleIndex < parser.RuleNames.Length) ? parser.RuleNames[ruleIndex] : null;
    }

    private static IReadOnlyList<string> GetRuleStack(Parser parser, ParserRuleContext ctx)
    {
        var stack = new List<string>();
        for (ParserRuleContext? c = ctx; c != null; c = c.Parent as ParserRuleContext)
        {
            var name = SafeRuleName(parser, c.RuleIndex);
            stack.Add(name ?? c.RuleIndex.ToString());
        }
        stack.Reverse(); // outermost -> innermost
        return stack;
    }

    private static IReadOnlyList<string> GetExpectedTokenNames(Parser parser)
    {
        var vocab = parser.Vocabulary;
        var set = parser.GetExpectedTokens(); // IntervalSet

        var names = new List<string>();
        foreach (var t in set.ToArray())
        {
            // Prefer literal name (e.g. "')'"), then symbolic (e.g. IDENT), else number
            names.Add(vocab.GetLiteralName(t) ?? vocab.GetSymbolicName(t) ?? t.ToString());
        }
        return names;
    }
}
