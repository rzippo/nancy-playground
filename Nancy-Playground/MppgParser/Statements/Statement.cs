using Antlr4.Runtime;
using Unipi.MppgParser.Visitors;

namespace Unipi.MppgParser;

public abstract class Statement
{
    public string Text { get; init; } = string.Empty;

    public abstract string Execute(State state);
    
    public abstract StatementOutput ExecuteToFormattable(State state);

    public static Statement FromLine(string line)
    {
        var inputStream = CharStreams.fromString(line);
        var lexer = new Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Grammar.MppgParser(commonTokenStream);
        
        var context = parser.statement();
        var visitor = new StatementVisitor();
        var statement = visitor.Visit(context);
        return statement;
    }
}