using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The parses of one line, and of one expression, are anchored at the end of the input by their own
/// entry rules, so that what is left over is reported rather than dropped.
/// The rules they wrap cannot carry that anchor, being nested inside a program.
/// </summary>
public class EntryRuleAnchoring
{
    [Theory]
    [InlineData("1 + 2 junk")]
    [InlineData("1 + 2 )")]
    [InlineData("1 + 2 3")]
    public void ExpressionRejectsWhatFollowsIt(string expression)
    {
        Assert.ThrowsAny<Exception>(() => ExpressionParsing.Parse(expression, null));
    }

    [Theory]
    [InlineData("1 + 2")]
    [InlineData("bucket(2, 5)")]
    public void ExpressionTakesWhatIsWholeOnItsOwn(string expression)
    {
        Assert.NotNull(ExpressionParsing.Parse(expression, null));
    }

    [Theory]
    [InlineData("x := 1 2")]
    [InlineData("x := 1 )")]
    public void StatementRejectsWhatFollowsIt(string line)
    {
        Assert.Throws<SyntaxErrorException>(() => Statement.FromLine(line));
    }

    /// <summary>
    /// An inline comment is part of the line, so it is not what is left over.
    /// </summary>
    [Theory]
    [InlineData("x := 1 // a comment")]
    [InlineData("x := 1 % a comment")]
    [InlineData("x := 1")]
    public void StatementTakesTheWholeLine(string line)
    {
        Assert.NotNull(Statement.FromLine(line));
    }
}
