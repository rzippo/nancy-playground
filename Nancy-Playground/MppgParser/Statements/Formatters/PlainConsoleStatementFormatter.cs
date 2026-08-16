using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Visitors;

namespace Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

/// <summary>
/// The output style of the original console, which writes a value as the syntax writes it.
/// </summary>
public class PlainConsoleStatementFormatter: IStatementFormatter
{
    /// <summary>
    /// Where the output is written, or null to write to the console as it is when writing, rather
    /// than as it was when this formatter was built.
    /// </summary>
    public TextWriter? Out { get; init; }

    private TextWriter Writer => Out ?? Console.Out;

    public void FormatStatementPreamble(Statement statement)
    {
        if(statement is not Comment)
            Writer.WriteLine(statement.Text);
    }

    public void FormatStatementOutput(Statement statement, StatementOutput output)
    {
        foreach (var warning in statement.Warnings)
            Writer.WriteLine(warning);

        if (output is PlotOutput)
            Writer.WriteLine(">> Plots are not rendered in this output mode.");
        else
            Writer.WriteLine($">> {ToMppgText(output)}");
    }

    /// <summary>
    /// The value of <paramref name="output"/> as the syntax writes it, rather than as the C# that
    /// builds it, which is what <see cref="StatementOutput.OutputText"/> carries.
    /// An assignment keeps its text, which is the name it assigned.
    /// </summary>
    private static string ToMppgText(StatementOutput output)
        => output switch
        {
            AssignmentOutput => output.OutputText,
            ExpressionOutput { Expression: CurveExpression curve } => curve.Value.ToMppgString(),
            ExpressionOutput { Expression: RationalExpression rational } => rational.Value.ToMppgString(),
            // printExpression shows the expression itself, which the syntax spells its own way
            PrintExpressionOutput print => ToMppgText(print.Expression, print.OutputText),
            _ => output.OutputText
        };

    /// <summary>
    /// The expression as MPPG source text, or <paramref name="fallback"/> when it uses an operation
    /// the syntax has no notation for, which the renderer reports rather than write something that
    /// would not parse.
    /// </summary>
    private static string ToMppgText(IExpression expression, string fallback)
    {
        try
        {
            return expression.ToMppgString();
        }
        catch (MppgFormattingException)
        {
            return fallback;
        }
    }

    public void FormatError(Statement statement, ErrorOutput error)
    {
        Writer.WriteLine($"Error: {error.Exception.Message}");
    }

    public void FormatEndOfProgram()
    {
        Writer.WriteLine(">> end of program");
    }
}