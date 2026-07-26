using Antlr4.Runtime;
using Unipi.Nancy.Playground.MppgParser;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public abstract record class Statement
{
    public string Text { get; init; } = string.Empty;

    public string InlineComment { get; init; } = string.Empty;

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
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(commonTokenStream);
        parser.ErrorHandler = new BailErrorStrategy();
        parser.SeedVariableTypes(state);
        parser.SetSyntaxVersion(syntaxVersion.Major, syntaxVersion.Minor);

        var context = parser.statement();
        var visitor = new StatementVisitor();
        var statement = visitor.Visit(context);
        return statement ?? throw new SyntaxErrorException("Statement could not be parsed.");
    }
}
