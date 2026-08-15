using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The output style of the original console writes a value as the syntax writes it, so that it can be
/// read back, rather than as the C# that builds it.
/// </summary>
public class MppgClassicOutputTests
{
    private static string Run(string programText)
    {
        var writer = new StringWriter();
        var formatter = new PlainConsoleStatementFormatter { Out = writer };
        var program = Program.FromText(programText);

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        while (!program.IsEndOfProgram)
            program.ExecuteNextStatement(formatter, immediateComputeValue: true);

        return writer.ToString().ReplaceLineEndings("\n");
    }

    [Theory]
    // a curve with a named constructor is written with it, in the argument order of the syntax
    [InlineData("bucket(2, 5)", "bucket(2, 5)")]
    [InlineData("ratency(1, 3)", "ratency(1, 3)")]
    [InlineData("zero", "zero")]
    [InlineData("epsilon", "epsilon")]
    // a scalar is written as the syntax writes it too
    [InlineData("1/2", "1/2")]
    public void ValueIsWrittenAsTheSyntaxWritesIt(string expression, string expected)
    {
        var output = Run($"x := {expression}\nx");

        Assert.Contains($">> {expected}", output);
    }

    [Fact]
    public void SampledValueIsWrittenAsAScalar()
    {
        var output = Run("f := bucket(2, 5)\nf(10)");

        Assert.Contains(">> 25", output);
    }

    /// <summary>
    /// A curve that no constructor names is written as the uaf or upp of its elements, which is the
    /// form the original console prints.
    /// </summary>
    [Fact]
    public void ComputedCurveIsWrittenAsItsElements()
    {
        var output = Run("f := bucket(2, 5)\ng := ratency(1, 3)\nh := f * g\nh");

        Assert.Contains(">> uaf([(0, 0)] ](0, 0) 0 (3, 0)[ [(3, 0)] ](3, 0) 1 (+inf, +inf)[)", output);
    }

    /// <summary>
    /// What is written parses back to the same value, which is what makes the form worth printing.
    /// </summary>
    [Fact]
    public void WhatIsWrittenParsesBackToTheSameValue()
    {
        var written = Run("h := bucket(2, 5) * ratency(1, 3)\nh")
            .Split('\n')
            .Single(line => line.StartsWith(">> uaf("))[3..];

        var roundTrip = Run($"h := {written}\nassert( h = bucket(2, 5) * ratency(1, 3) )");

        Assert.Contains(">> true", roundTrip);
    }

    /// <summary>
    /// An assignment prints the name it assigned, as the original console does.
    /// </summary>
    [Fact]
    public void AssignmentIsWrittenAsTheNameItAssigned()
    {
        var output = Run("f := bucket(2, 5)");

        Assert.Contains(">> f", output);
    }
}
