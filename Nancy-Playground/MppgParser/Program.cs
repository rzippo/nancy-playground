using Antlr4.Runtime;
using Unipi.MppgParser.Visitors;

namespace Unipi.MppgParser;

public class Program
{
    public List<Statement> Statements { get; init; }

    public int ProgramCounter { get; private set; } = 0;
    
    public State State { get; init; } =  new State();

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
        return program;
    }

    public static Program FromText(string text)
    {
        var inputStream = CharStreams.fromString(text);
        var lexer = new Grammar.MppgLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new Grammar.MppgParser(commonTokenStream);
        
        var context = parser.program();
        return FromTree(context);
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
        yield return statement.Execute(State);
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
            formatter.FormatStatementPreamble(statement);
            try
            {
                var output = statement switch
                {
                    Assignment assignment => assignment.ExecuteToFormattable(State, immediateComputeValue),
                    _ => statement.ExecuteToFormattable(State)
                };
                formatter.FormatStatementOutput(statement, output);
                return output;
            }
            catch (Exception e)
            {
                var error = new ErrorOutput
                {
                    StatementText = statement.Text,
                    OutputText = string.Empty,
                    Exception = e
                };
                formatter.FormatError(statement, error);
                return error;
            }
        }
    }
}