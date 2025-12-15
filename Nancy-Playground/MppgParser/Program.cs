using Antlr4.Runtime;
using Unipi.MppgParser.Utility;
using Unipi.MppgParser.Visitors;

namespace Unipi.MppgParser;

public record class Program
{
    /// <summary>
    /// The original text of the program.
    /// </summary>
    public string Text { get; init; }
    /// <summary>
    /// The list of statements in the program.
    /// </summary>
    public List<Statement> Statements { get; init; }
    /// <summary>
    /// The current program counter.
    /// </summary>
    public int ProgramCounter { get; private set; } = 0;
    /// <summary>
    /// The program execution context.
    /// </summary>
    public ProgramContext ProgramContext { get; init; } =  new ();

    /// <summary>
    /// True if there are no more program statements to execute.
    /// </summary>
    public bool IsEndOfProgram 
        => ProgramCounter >= Statements.Count;
    
    public Program(List<Statement> statements)
    {
        Statements = statements;
        Text = statements
            .Select(s => s.Text)
            .JoinText("\n");
    }

    /// <summary>
    /// Parses the MPPG program from its parse tree and returns the corresponding Program object.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static Program FromTree(Grammar.MppgParser.ProgramContext context)
    {
        var visitor = new ProgramVisitor();
        var program = visitor.Visit(context);
        return program with
        {
            Text = context.GetJoinedText()
        };
    }

    /// <summary>
    /// Parses MPPG program text and returns the corresponding Program object.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Executes the entire program and returns its string output.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> ExecuteToStringOutput()
    {
        while (ProgramCounter < Statements.Count)
        {
            var statementOutput = ExecuteNextStatementToStringOutput();
            foreach (var line in statementOutput)
                yield return line;
        }
    }

    /// <summary>
    /// Executes the next statement in the program and returns its string output.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> ExecuteNextStatementToStringOutput()
    {
        if(IsEndOfProgram)
            yield return $">> end of program";
        
        var statement = Statements[ProgramCounter++];
        if (statement is not Comment)
            yield return $">> {statement.Text}";
        yield return statement.Execute(ProgramContext.State);
    }

    /// <summary>
    /// Executes the next statement in the program.
    /// </summary>
    /// <param name="formatter"></param>
    /// <param name="immediateComputeValue"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Converts the MPPG program to Nancy code.
    /// </summary>
    /// <param name="useNancyExpressions"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public List<string> ToNancyCode(bool useNancyExpressions = false)
    {
        if (Text.IsNullOrWhiteSpace())
            throw new InvalidOperationException("Program text not available!");
        
        return ToNancyCode(Text,  useNancyExpressions);
    }
    
    /// <summary>
    /// Converts MPPG program text to Nancy code.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="useNancyExpressions"></param>
    /// <returns></returns>
    public static List<string> ToNancyCode(
        string text, 
        bool useNancyExpressions = false
    )
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