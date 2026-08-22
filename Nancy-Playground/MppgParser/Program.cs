using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;
using Unipi.Nancy.Playground.MppgParser.Utility;
using Unipi.Nancy.Playground.MppgParser.Visitors;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// An MPPG program, i.e. its statements, the errors found parsing them, and the session they run against.
/// </summary>
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
    /// Errors collected by ANTLR during parsing of this program.
    /// </summary>
    public List<SyntaxErrorInfo> Errors { get; init; }

    /// <summary>
    /// The syntax version declared by the program (via #!syntax shebang) or Latest if unspecified.
    /// </summary>
    public SyntaxVersion SyntaxVersion { get; init; } = SyntaxVersion.Latest;

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

    /// <summary>
    /// A program made of the given statements.
    /// </summary>
    public Program(List<Statement> statements)
    {
        if (statements.Any(static s => s is null))
            throw new ArgumentException("Program statements cannot contain null.", nameof(statements));

        Statements = statements;
        Text = statements
            .Select(s => s.Text)
            .JoinText("\n");
        Errors = [];
    }

    /// <summary>
    /// Parses the MPPG program from its parse tree and returns the corresponding Program object.
    /// </summary>
    /// <param name="context">The parse tree of the program.</param>
    /// <param name="syntaxErrors">The errors collected while parsing, which the program carries to be reported.</param>
    /// <param name="syntaxVersion">The version the program was parsed with, or the default for the latest.</param>
    public static Program FromTree(
        Unipi.MppgParser.Grammar.MppgParser.ProgramContext context,
        IReadOnlyList<SyntaxErrorInfo>? syntaxErrors = null,
        SyntaxVersion syntaxVersion = default)
    {
        if (syntaxVersion == default)
            syntaxVersion = SyntaxVersion.Latest;

        var visitor = new ProgramVisitor(syntaxErrors, syntaxVersion);
        var program = visitor.Visit(context);
        return program with
        {
            Text = context.GetJoinedText(),
            SyntaxVersion = syntaxVersion
        };
    }

    /// <summary>
    /// Parses MPPG program text and returns the corresponding Program object.
    /// </summary>
    public static Program FromText(string text)
    {
        var parse = MppgParsing.Create(text, ErrorRecovery.CollectAll);

        var context = parse.Parser.program();
        ReportUnusableVersionDirectives(context, text, parse.Errors);
        var program = FromTree(context, parse.Errors, parse.DeclaredSyntaxVersion);
        return program with
        {
            Text = text,
            Errors = parse.Errors
        };
    }

    /// <summary>
    /// Reports the version directives that declare a version this build cannot apply.
    /// They come first among the errors, being the cause of the ones the gating of a wrong version
    /// produces.
    /// </summary>
    private static void ReportUnusableVersionDirectives(
        Unipi.MppgParser.Grammar.MppgParser.ProgramContext context,
        string text,
        List<SyntaxErrorInfo> errors
    )
    {
        var directives = context.preamble()?.preambleStatement()
            .Select(statement => statement.versionDirective())
            .Where(directive => directive is not null)
            .ToList();
        if (directives is null || directives.Count == 0)
            return;

        var reported = new List<SyntaxErrorInfo>();
        foreach (var directive in directives)
        {
            var directiveText = directive!.GetText();
            if (VersionDirective.Read(directiveText, out var error) is not null)
                continue;

            var token = directive.Start;
            var lines = token.StartIndex >= 0
                ? SourceLineExtractor.ExtractLines(text, token.StartIndex)
                : null;

            var hint = SyntaxVersion.TryParseShebang(directiveText, out var declared) && declared > SyntaxVersion.Latest
                ? VersionDirective.TooRecentHint(declared, hasOtherErrors: errors.Count > 0)
                : null;

            reported.Add(new SyntaxErrorInfo(
                Line: token.Line,
                Column: token.Column,
                Message: error!,
                Type: SyntaxErrorInfo.ErrorType.Parser,
                OffendingText: directiveText,
                OffendingTokenType: token.Type,
                RuleName: null,
                RuleStack: null,
                Expected: null,
                SourceLine: lines?.Line,
                PreviousLine: lines?.Previous,
                Hint: hint
            ));
        }

        errors.InsertRange(0, reported);
    }

    /// <summary>
    /// Executes the entire program and returns its string output.
    /// </summary>
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
    public IEnumerable<string> ExecuteNextStatementToStringOutput()
    {
        if(IsEndOfProgram)
            yield return $">> end of program";

        var statement = Statements[ProgramCounter++];
        if (statement is not Comment)
            yield return $">> {statement.Text}";

        // over the same path the formatters use, so that the two cannot write a value differently
        string text;
        try
        {
            text = statement.ExecuteToFormattable(ProgramContext.State).OutputText;
        }
        catch (Exception e)
        {
            text = e.Message;
        }
        yield return text;
    }

    /// <summary>
    /// Executes the next statement in the program.
    /// </summary>
    /// <param name="formatter">Where to write what the statement produced.</param>
    /// <param name="immediateComputeValue">True to compute the value of the statement as it runs, false to build the expression and leave it to be evaluated when required.</param>
    /// <returns>The output of the statement, or null at the end of the program.</returns>
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
    /// <param name="useNancyExpressions">True to emit code that builds expressions with Unipi.Nancy.Expressions, false for the Nancy API, which computes values.</param>
    /// <returns>The lines of the generated program.</returns>
    /// <exception cref="InvalidOperationException">The program was built from a parse tree, so it has no source text to convert.</exception>
    public List<string> ToNancyCode(bool useNancyExpressions = false)
    {
        if (Text.IsNullOrWhiteSpace())
            throw new InvalidOperationException("Program text not available!");

        return ToNancyCode(Text,  useNancyExpressions);
    }

    /// <summary>
    /// Converts MPPG program text to Nancy code.
    /// </summary>
    /// <param name="text">The program to convert.</param>
    /// <param name="useNancyExpressions">True to emit code that builds expressions with Unipi.Nancy.Expressions, false for the Nancy API, which computes values.</param>
    /// <returns>The lines of the generated program.</returns>
    /// <exception cref="Exceptions.SyntaxErrorException">The text does not parse.</exception>
    public static List<string> ToNancyCode(
        string text,
        bool useNancyExpressions = false
    )
    {
        var parse = MppgParsing.Create(text, ErrorRecovery.FirstError);

        var programContext = parse.ParseOrThrow(static parser => parser.program());
        MppgBaseVisitor<List<string>> visitor = useNancyExpressions
            ? new ToNancyExpressionsCodeVisitor(parse.DeclaredSyntaxVersion)
            : new ToNancyCodeVisitor(parse.DeclaredSyntaxVersion);
        var code = programContext.Accept(visitor);

        return code;
    }
}
