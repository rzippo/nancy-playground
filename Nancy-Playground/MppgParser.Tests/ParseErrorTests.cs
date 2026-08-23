namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The facts an error carries, which are what the matchers read.
/// </summary>
/// <remarks>
/// Held apart from what is written about them: a fact read wrongly makes every matcher above it wrong.
/// </remarks>
public class ParseErrorTests
{
    private static ParseError FirstError(string programText)
        => Assert.IsAssignableFrom<ParseError>(Program.FromText(programText).Errors[0].Source);

    /// <summary>
    /// A token the parser invents and one it drops are reported the same way, with no exception, so the recovery it recorded is the only thing that tells them apart.
    /// </summary>
    [Theory]
    // a bracket left open, and a separator left out, which the parser carries on past by inventing one
    [InlineData("f := bucket(2, 5", ParserRecovery.MissingToken)]
    [InlineData("f := bucket(2 5)", ParserRecovery.MissingToken)]
    // a bracket too many, which it carries on past by dropping it
    [InlineData("f := bucket(2, 5))", ParserRecovery.UnwantedToken)]
    internal void TheRecoveryOfTheParserIsRecorded(string programText, ParserRecovery expected)
    {
        var error = Assert.IsType<ParserError>(FirstError(programText));

        Assert.Equal(expected, error.Recovery);
    }

    /// <summary>
    /// An error the parser raises is not a recovery, so it carries the exception instead.
    /// </summary>
    [Fact]
    public void AnErrorThatIsRaisedRecordsNoRecovery()
    {
        var error = Assert.IsType<ParserError>(FirstError("g := f + 1"));

        Assert.Equal(ParserRecovery.None, error.Recovery);
        Assert.NotNull(error.Exception);
    }

    /// <summary>
    /// The kinds are told apart by their type rather than by a field saying which one they are.
    /// </summary>
    [Theory]
    [InlineData("x := 1 @ 2", typeof(LexerError))]
    [InlineData("g := f + 1", typeof(ParserError))]
    [InlineData("#!syntax version 9.9", typeof(UnusableVersionDirectiveError))]
    public void EachErrorIsOfTheKindThatReportedIt(string programText, Type kind)
    {
        Assert.IsType(kind, FirstError(programText));
    }
}
