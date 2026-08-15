using Antlr4.Runtime;
using Unipi.Nancy.Playground.MppgParser;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Utility;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public abstract record class Statement
{
    public string Text { get; init; } = string.Empty;

    public string InlineComment { get; init; } = string.Empty;

    /// <summary>
    /// Diagnostics about the statement that do not stop it from running, reported by the formatters alongside its output.
    /// See <see cref="Utility.ScalarDivisionGrouping"/> for the one case that currently produces them.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public abstract string Execute(State state);

    public abstract StatementOutput ExecuteToFormattable(State state);

    public static Statement FromLine(string line, State? state = null)
    {
        return FromLine(line, state, SyntaxVersion.Latest);
    }

    public static Statement FromLine(string line, State? state, SyntaxVersion syntaxVersion)
    {
        var parse = MppgParsing.Create(line, ErrorRecovery.FirstError, syntaxVersion, state);

        var context = parse.ParseOrThrow(static parser => parser.statement());

        CheckWholeLineWasParsed(parse.Tokens, parse.Errors);
        parse.ThrowIfErrors();

        var visitor = new StatementVisitor();
        var statement = visitor.Visit(context)
            ?? throw new SyntaxErrorException("Statement could not be parsed.");

        return statement with { Warnings = ScalarDivisionGrouping.WarningsFor(context) };
    }

    /// <summary>
    /// Rejects a line the statement rule stopped short of.
    /// Its empty alternative matches without consuming anything, so a line that starts with something no
    /// statement can start with would otherwise be read as an empty statement rather than reported.
    /// </summary>
    private static void CheckWholeLineWasParsed(ITokenStream tokens, IList<SyntaxErrorInfo> errors)
    {
        var next = tokens.LT(1);
        if (next.Type == TokenConstants.EOF
            || next.Type == Unipi.MppgParser.Grammar.MppgLexer.INLINABLE_COMMENT)
            return;

        var text = SourceLineExtractor.GetText(next.TokenSource?.InputStream);
        var lines = text is not null && next.StartIndex >= 0
            ? SourceLineExtractor.ExtractLines(text, next.StartIndex)
            : null;

        errors.Add(new SyntaxErrorInfo(
            Line: next.Line,
            Column: next.Column,
            Message: $"Unexpected input '{next.Text}'.",
            Type: SyntaxErrorInfo.ErrorType.Parser,
            OffendingText: next.Text,
            OffendingTokenType: next.Type,
            RuleName: null,
            RuleStack: null,
            Expected: null,
            SourceLine: lines?.Line,
            PreviousLine: lines?.Previous,
            Hint: VersionedKeywords.TryGetUsedAsNameHint(VersionedKeywords.TokensOfLine(tokens, next))
        ));
    }
}
