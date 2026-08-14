using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class SyntaxErrorInfoTests
{
    [Fact]
    public void ErrorAtLineStartCarriesTheLineBeforeIt()
    {
        var program = Program.FromText("f := bucket(2, 5)\n)");

        var error = Assert.Single(program.Errors);
        Assert.Equal("f := bucket(2, 5)", error.PreviousLine);
        Assert.Equal(")", error.SourceLine);
        Assert.Equal(0, error.Column);
        Assert.Equal(")", error.OffendingText);
    }

    [Fact]
    public void ErrorMidLineCarriesTheOffendingLine()
    {
        var program = Program.FromText("f := bucket(2, 5)\ng := f + missing");

        var error = Assert.Single(program.Errors);
        Assert.Equal("f := bucket(2, 5)", error.PreviousLine);
        Assert.Equal("g := f + missing", error.SourceLine);
        Assert.Equal(9, error.Column);
        Assert.Equal("missing", error.OffendingText);
    }
}
