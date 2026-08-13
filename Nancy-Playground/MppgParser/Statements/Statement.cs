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
        var inputStream = CharStreams.fromString(line);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        lexer.SetSyntaxVersion(syntaxVersion.Major, syntaxVersion.Minor);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);
        parser.ErrorHandler = new BailErrorStrategy();
        parser.SeedVariableTypes(state);

        Unipi.MppgParser.Grammar.MppgParser.StatementContext context;
        try
        {
            context = parser.statement();
            EnsureWholeLineWasParsed(commonTokenStream);
        }
        catch (Exception ex)
        {
            throw ex.WithVersionedKeywordHint(commonTokenStream);
        }

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
    private static void EnsureWholeLineWasParsed(ITokenStream tokens)
    {
        var next = tokens.LT(1);
        if (next.Type == TokenConstants.EOF
            || next.Type == Unipi.MppgParser.Grammar.MppgLexer.INLINABLE_COMMENT)
            return;

        throw new SyntaxErrorException($"Unexpected input '{next.Text}'.");
    }
}
