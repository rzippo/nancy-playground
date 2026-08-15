using Unipi.Nancy.Playground.MppgParser.Exceptions;
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

    [Fact]
    public void StatementErrorCarriesItsLine()
    {
        var exception = Assert.Throws<SyntaxErrorException>(() => Statement.FromLine("f := bucket(2, 5))"));

        var error = exception.Error;
        Assert.NotNull(error);
        Assert.Equal("f := bucket(2, 5))", error.SourceLine);
        Assert.Equal(")", error.OffendingText);
    }

    [Fact]
    public void ConversionErrorCarriesItsLine()
    {
        var exception = Assert.Throws<SyntaxErrorException>(
            () => Program.ToNancyCode("f := bucket(2, 5)\ng := f + missing"));

        var error = exception.Error;
        Assert.NotNull(error);
        Assert.Equal(2, error.Line);
        Assert.Equal(9, error.Column);
        Assert.Equal("g := f + missing", error.SourceLine);
        Assert.Equal("missing", error.OffendingText);
    }

    [Fact]
    public void ConversionReportsTheEarliestFailure()
    {
        const string text = "f := bucket(2, 5\ng := ]";

        var exception = Assert.Throws<SyntaxErrorException>(() => Program.ToNancyCode(text));

        Assert.Equal(1, exception.Error?.Line);
        // the same text, parsed to be run, collects the errors of both lines
        Assert.Equal(2, Program.FromText(text).Errors.Count);
    }
}
