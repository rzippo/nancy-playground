using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// A session, i.e. the variables declared so far and the statements that declared them.
/// </summary>
public class ProgramContext
{
    /// <summary>
    /// The variables declared so far.
    /// </summary>
    public State State { get; init; } =  new ();

    /// <summary>
    /// The statements run so far, in order, which is what writing the session back to a file uses.
    /// </summary>
    public List<Statement> StatementHistory { get; init; } =  new ();

    /// <summary>
    /// The version the session reads its statements with.
    /// </summary>
    public SyntaxVersion SyntaxVersion { get; set; } = SyntaxVersion.Latest;

    /// <summary>
    /// True once a syntax version directive has been applied to this context.
    /// </summary>
    public bool SyntaxVersionDirectiveApplied { get; set; } = false;

    /// <summary>
    /// True while a syntax version directive can still be applied, i.e. before any statement is executed.
    /// This matches whole-program parsing, where only a directive in the preamble is applied, so that a session behaves the same once exported and run again.
    /// </summary>
    public bool CanApplySyntaxVersionDirective =>
        !SyntaxVersionDirectiveApplied && StatementHistory.Count == 0;

    /// <summary>
    /// The lines of this session as a program, including the syntax version directive if one was applied.
    /// </summary>
    /// <remarks>
    /// Applying the directive does not execute a statement, so it is not in the statement history and has
    /// to be put back here, for the exported program to behave the same when run again.
    /// </remarks>
    public IEnumerable<string> ToProgramLines()
    {
        var statementLines = StatementHistory.Select(s => s.Text);

        return SyntaxVersionDirectiveApplied
            ? statementLines.Prepend($"#!syntax version {SyntaxVersion}")
            : statementLines;
    }

    /// <summary>
    /// Runs <paramref name="statement"/> against the session and hands what it produced to <paramref name="formatter"/>.
    /// <paramref name="immediateComputeValue"/> computes the value there and then, rather than leaving it to be evaluated when required.
    /// </summary>
    public StatementOutput? ExecuteStatement(
        Statement statement,
        IStatementFormatter formatter,
        bool immediateComputeValue
    )
    {
        formatter.FormatStatementPreamble(statement);
        try
        {
            StatementHistory.Add(statement);
            var output = statement switch
            {
                Assignment assignment => assignment.ExecuteToFormattable(State, immediateComputeValue),
                _ => statement.ExecuteToFormattable(State)
            };
            formatter.FormatStatementOutput(statement, output);
            return output;
        }
#pragma warning disable CS0168 // Variable is declared but never used
        catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
        {
            #if DEBUG
            throw;
            #else
            var error = new ErrorOutput
            {
                StatementText = statement.Text,
                OutputText = string.Empty,
                Exception = e
            };
            formatter.FormatError(statement, error);
            return error;
            #endif
        }
    }
}