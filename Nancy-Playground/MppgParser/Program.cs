using Antlr4.Runtime;
using Unipi.MppgParser.Utility;
using Unipi.MppgParser.Visitors;

namespace Unipi.MppgParser;

public record class Program
{
    public string Text { get; init; }

    public List<Statement> Statements { get; init; }

    public int ProgramCounter { get; private set; } = 0;
    
    public ProgramContext ProgramContext { get; init; } =  new ();

    /// <summary>
    /// True if there are no more program statements to execute.
    /// </summary>
    public bool IsEndOfProgram 
        => ProgramCounter >= Statements.Count;
    
    public Program(List<Statement> statements)
    {
        Statements = statements;
    }

    public static Program FromTree(Grammar.MppgParser.ProgramContext context)
    {
        var visitor = new ProgramVisitor();
        var program = visitor.Visit(context);
        return program with
        {
            Text = context.GetJoinedText()
        };
    }

    public static Program FromText(string text)
    {
        var inputStream = CharStreams.fromString(text);
        var lexer = new Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Grammar.MppgParser(commonTokenStream);
        
        var context = parser.program();
        var program = FromTree(context);
        return program with
        {
            Text = text
        };
    }

    public IEnumerable<string> ExecuteToStringOutput()
    {
        while (ProgramCounter < Statements.Count)
        {
            var statementOutput = ExecuteNextStatementToStringOutput();
            foreach (var line in statementOutput)
                yield return line;
        }
    }

    public IEnumerable<string> ExecuteNextStatementToStringOutput()
    {
        if(IsEndOfProgram)
            yield return $">> end of program";
        
        var statement = Statements[ProgramCounter++];
        if (statement is not Comment)
            yield return $">> {statement.Text}";
        yield return statement.Execute(ProgramContext.State);
    }

    public StatementOutput? ExecuteNextStatement(
        IStatementFormatter formatter,
        bool immediateComputeValue
    )
    {
        if (IsEndOfProgram)
        {
            formatter.FormatEndOfProgram();
            return null;
        }
        else
        {
            var statement = Statements[ProgramCounter++];
            return ProgramContext.ExecuteStatement(
                statement, formatter, immediateComputeValue);
        }
    }

    public List<string> ToNancyCode(bool useNancyExpressions = false)
    {
        if (Text.IsNullOrWhiteSpace())
            throw new InvalidOperationException("Program text not available!");
        
        return ToNancyCode(Text,  useNancyExpressions);
    }
    
    public static List<string> ToNancyCode(string text, bool useNancyExpressions = false)
    {
        var inputStream = CharStreams.fromString(text);
        var lexer = new Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Grammar.MppgParser(commonTokenStream);

        var programContext = parser.program();
        var visitor = new ToNancyCodeVisitor();
        var code = programContext.Accept(visitor);
        
        return code;
    }
}