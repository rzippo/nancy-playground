using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// One syntax error, with everything needed to report it: where it is, what to say about it, and the source around it.
/// </summary>
/// <param name="Line">The line the error is on, counted from one.</param>
/// <param name="Column">The column the error is at, counted from zero.</param>
/// <param name="Message">What to tell the reader, rewritten from what ANTLR said where a pattern recognised the error.</param>
/// <param name="Type">Whether the lexer or the parser reported it.</param>
/// <param name="OffendingText">The token, or the character, that could not be used.</param>
/// <param name="OffendingTokenType">Its token type, or null for an error of the lexer.</param>
/// <param name="RuleName">The rule being parsed, or null for an error of the lexer.</param>
/// <param name="RuleStack">The rules it was nested in, outermost first.</param>
/// <param name="Expected">The tokens that would have fitted, which only the parser knows.</param>
/// <param name="SourceLine">The line the error is on, raw and unescaped: a caret or a colour is for the display layer to add.</param>
/// <param name="PreviousLine">The line before it, for the display layer to show as context.</param>
/// <param name="Hint">What likely caused the error, where it can be told apart from the message alone.</param>
/// <param name="AntlrMessage">What ANTLR said, kept when <paramref name="Message"/> was rewritten.</param>
/// <param name="RewrittenBy">
/// What recognised the error and wrote <paramref name="Message"/>, or null where nothing did and the message is the one the error came with.
/// </param>
public sealed record SyntaxErrorInfo(
    int Line,
    int Column,
    string Message,
    SyntaxErrorInfo.ErrorType Type,
    string? OffendingText,
    int? OffendingTokenType,
    string? RuleName,
    IReadOnlyList<string>? RuleStack,
    IReadOnlyList<string>? Expected,
    string? SourceLine,
    string? PreviousLine,
    string? Hint = null,
    string? AntlrMessage = null,
    string? RewrittenBy = null
)
{
    /// <summary>
    /// The error this reports, i.e. what was known of it before anything was written about it.
    /// </summary>
    /// <remarks>
    /// Kept so that the matchers can be held to what they recognise, which is what the tests read it for, and so that a report can be traced back to its facts.
    /// </remarks>
    internal ParseError? Source { get; init; }

    /// <summary>
    /// What reported the error.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// The lexer, which reads characters into tokens.
        /// </summary>
        Lexer,

        /// <summary>
        /// The parser, which reads tokens into rules.
        /// </summary>
        Parser,

        /// <summary>
        /// The playground itself, for a directive the grammar accepts but this build cannot apply.
        /// </summary>
        Directive
    }

    /// <summary>
    /// The report of <paramref name="error"/>, i.e. what it knows together with what is written about it, which is the one place a <see cref="SyntaxErrorInfo"/> is built.
    /// </summary>
    /// <remarks>
    /// The message and the hint of a pattern come first, and what the error carries is the fallback, so that an error no pattern recognises still reads as well as it can.
    /// The patterns are read here rather than at each producer, which is what keeps the message and the facts of one and the same error together.
    /// </remarks>
    /// <param name="error">The error to report.</param>
    /// <param name="rewrite">
    /// False to keep what the error carries, without looking for a pattern that recognises it.
    /// </param>
    internal static SyntaxErrorInfo From(ParseError error, bool rewrite = true)
    {
        var rewritten = rewrite ? SyntaxErrorMessages.Rewrite(error) : null;
        var parser = error as ParserError;
        return new SyntaxErrorInfo(
            Line: error.Position.Line,
            Column: error.Position.Column,
            Message: rewritten?.Message ?? error.DefaultMessage,
            Type: error switch
            {
                LexerError => ErrorType.Lexer,
                UnusableVersionDirectiveError => ErrorType.Directive,
                _ => ErrorType.Parser
            },
            OffendingText: error.OffendingText,
            OffendingTokenType: parser?.Tokens.Offending?.Type,
            RuleName: parser?.Rule.Name,
            RuleStack: parser?.Rule.Stack,
            Expected: parser?.Expected.Names,
            SourceLine: error.Position.SourceLine,
            PreviousLine: error.Position.PreviousLine,
            Hint: rewritten?.Hint ?? error.DefaultHint,
            AntlrMessage: error.AntlrMessage,
            RewrittenBy: rewritten?.WrittenBy
        )
        {
            Source = error
        };
    }

    /// <summary>
    /// The error as one line, i.e. its position and its message.
    /// </summary>
    public override string ToString()
        => ToString(false);

    /// <summary>
    /// The error as one line, adding the rule and the expected tokens when <paramref name="verbose"/> is true.
    /// </summary>
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

/// <summary>
/// Collects the errors of the lexer, i.e. the characters it could not read.
/// </summary>
public sealed class DiagnosticLexerErrorListener : IAntlrErrorListener<int>
{
    private readonly IList<SyntaxErrorInfo> _errors;
    private readonly ICharStream? _charStream;

    /// <summary>
    /// A listener adding to <paramref name="errors"/>, reading the source around each error from <paramref name="charStream"/>.
    /// </summary>
    public DiagnosticLexerErrorListener(IList<SyntaxErrorInfo> errors, ICharStream? charStream)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _charStream = charStream;
    }
    /// <summary>
    /// Records one error of the lexer, with the character it stopped at and the line it is on.
    /// </summary>
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        var text = SourceLineExtractor.GetText(_charStream);
        var charIndex = text is null
            ? null
            : SourceLineExtractor.CharIndexOfLine(text, line, charPositionInLine);
        var lines = text is not null && charIndex is int i
            ? SourceLineExtractor.ExtractLines(text, i)
            : null;

        // read from the input: a lexer reports the error with no offending symbol, passing zero
        var character = text is not null && charIndex is int index
            ? text[index].ToString()
            : null;

        var error = new LexerError(
            Position: new SourcePosition(line, charPositionInLine, lines?.Line, lines?.Previous),
            Character: character,
            LexerMessage: msg);

        var report = SyntaxErrorInfo.From(error);
        _errors.Add(report);
    }
}

/// <summary>
/// Collects the errors of the parser, i.e. the tokens it could not use.
/// </summary>
public sealed class DiagnosticParserErrorListener : BaseErrorListener
{
    private readonly IList<SyntaxErrorInfo> _errors;


    /// <summary>
    /// A listener adding to <paramref name="errors"/>.
    /// </summary>
    public DiagnosticParserErrorListener(IList<SyntaxErrorInfo> errors)
    {
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }
    /// <summary>
    /// Records one error of the parser, with the rule it was in and what would have fitted instead.
    /// </summary>
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        var text = SourceLineExtractor.GetText(offendingSymbol?.TokenSource?.InputStream);
        var lines = text is not null && offendingSymbol?.StartIndex is int start and >= 0
            ? SourceLineExtractor.ExtractLines(text, start)
            : null;
        var position = new SourcePosition(line, charPositionInLine, lines?.Line, lines?.Previous);

        if (recognizer is not Parser parser)
        {
            // no parser to ask, so the error carries only what the listener was handed
            var unrecognised = new ParserError(
                position,
                new ErrorTokens(offendingSymbol, null, null),
                ParsedRule.None,
                ExpectedTokens.None,
                e,
                ParserError.NoVariables,
                msg);

            var unrecognisedReport = SyntaxErrorInfo.From(unrecognised);
            _errors.Add(unrecognisedReport);
            return;
        }

        var context = parser.Context;
        var rule = context is null
            ? ParsedRule.None
            : new ParsedRule(SafeRuleName(parser, context.RuleIndex), GetRuleStack(parser, context), context.Start);

        var error = new ParserError(
            Position: position,
            Tokens: new ErrorTokens(
                offendingSymbol,
                TokenAround(parser, offendingSymbol, -1),
                TokenAround(parser, offendingSymbol, +1)),
            Rule: rule,
            Expected: new ExpectedTokens(GetExpectedTokenNames(parser) ?? [], parser.GetExpectedTokens().ToArray()),
            Exception: e,
            DeclaredVariables: parser is Unipi.MppgParser.Grammar.MppgParser mppgParser
                ? mppgParser.VariableTypes
                : ParserError.NoVariables,
            ParserMessage: msg,
            DefaultHint: VersionedKeywords.TryGetUsedAsNameHint(
                VersionedKeywords.TokensOfLine(parser.TokenStream, offendingSymbol)),
            ReadableMessage: QuotedFromSource(msg, e, offendingSymbol, text),
            Recovery: parser.ErrorHandler is RecordingErrorStrategy strategy
                ? strategy.Recovery
                : ParserRecovery.None,
            LineTokens: VersionedKeywords.TokensOfLine(parser.TokenStream, offendingSymbol).ToList());

        var report = SyntaxErrorInfo.From(error);
        _errors.Add(report);
    }

    /// <summary>
    /// The message of a viable-alternative error, quoting the source rather than the tokens joined together, or null where there is nothing to repair.
    /// </summary>
    /// <remarks>
    /// ANTLR quotes the span from the start of the alternative to the token it stopped at, taken from the tokens, so '( floor comp' comes out as '(floorcomp'.
    /// The span is read from the source instead, and clipped to the line the error is on, a span that opens on the line before being quoted as the newline it starts with.
    /// </remarks>
    private static string? QuotedFromSource(string message, RecognitionException? e, IToken? offending, string? text)
    {
        if (e is not NoViableAltException viable || offending is null || text is null)
            return null;

        var start = viable.StartToken?.StartIndex ?? offending.StartIndex;
        var stop = offending.StopIndex;
        if (start < 0 || stop < start || stop >= text.Length)
            return null;

        var span = text[start..(stop + 1)];
        var lastBreak = span.LastIndexOf('\n');
        if (lastBreak >= 0)
            span = span[(lastBreak + 1)..];

        span = span.Trim();
        return span.Length == 0 ? null : $"no viable alternative at input '{span}'";
    }

    /// <summary>
    /// The token <paramref name="offset"/> places from <paramref name="token"/> in the stream, or null where there is none.
    /// </summary>
    private static IToken? TokenAround(Parser parser, IToken? token, int offset)
    {
        if (token is null || parser.TokenStream is not { } tokens)
            return null;

        var index = token.TokenIndex + offset;
        return index >= 0 && index < tokens.Size ? tokens.Get(index) : null;
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
