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

        // the entry rule is anchored at EOF, so input left over on the line is reported by the parser
        var entry = parse.ParseOrThrow(static parser => parser.statementEntry());
        var context = entry.statementLine().statement();

        var visitor = new StatementVisitor();
        var statement = visitor.Visit(context)
            ?? throw new SyntaxErrorException("Statement could not be parsed.");

        return statement with { Warnings = ScalarDivisionGrouping.WarningsFor(context) };
    }
}
