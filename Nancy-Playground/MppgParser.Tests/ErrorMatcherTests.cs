namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The matchers as a set, rather than one by one: what they recognise must not overlap, none of them may be unreachable, and what they write must read as a fragment.
/// </summary>
/// <remarks>
/// <see cref="SyntaxErrorMessagesTests"/> pins what each one says.
/// These hold the registry itself, which is what a matcher written with too loose a guard breaks.
/// </remarks>
public class ErrorMatcherTests
{
    /// <summary>
    /// Programs that fail to parse, one per shape met so far, which the checks below are run over.
    /// </summary>
    public static TheoryData<string> Corpus =>
    [
        // a name that was never declared, in the places an expression can stand
        "g := f + 1",
        "g := f(2)",
        "f",
        "assert( f = 1 )",
        "g := 1 + f",
        // a keyword written where a name belongs, reported at either side of the assignment
        "div := 3",
        "comp := 3",
        "star := 3",
        "inv := 3",
        "#!syntax version 1.3\nfloor := 3",
        // a bracket or a separator left out
        "f := bucket(2, 5",
        "f := bucket(2, 5\ng := 1",
        "f := uaf( [(0,0)1(1,1)[ ",
        "f := bucket(2 5)",
        // an argument list closed too early, or carried on too long
        "f := bucket(2)",
        "f := stair(1, 2)",
        "f := bucket(2, 5)\ng := hShift(f)",
        "f := bucket(2, 5, 7)",
        "f := delay(1, 2)",
        "x := pow(2)",
        "x := abs(2, 3)",
        // something after a statement that was read whole
        "f := bucket(2, 5))",
        "x := 1 2",
        "x := 1 = 2",
        // an expression that runs out
        "g := 1 +",
        "f := bucket(2, 5)\ng := f *",
        "g := (1 + 2",
        "f := bucket(2, 5)\ng := f comp",
        // characters the lexer cannot read, written as a name and not
        "x := 1\ny := x ÷ 2",
        "x := 1 @ 2",
        "f := bucket(2, @)",
        "@ := 1",
        "ab@ := 1",
        "a@b := 1",
        "f := bucket(2, 5)\nplot(f, out=\"x)",
        // a token that could not stand where it did, where what was expected is named or spelled
        "x := ]",
        "x := * 2",
        "f := bucket(2, 5)\nplot(f, xlim=[1,])",
        "f := bucket(2, 5)\nassert(f)",
        // a token that could open what was expected, so naming it would read as a contradiction
        "f := bucket(2, 5)\ng := ((f + 1) (f - 1))",
        // shapes met while reviewing, claimed or not, which the checks above hold either way
        "f := bucket(2, 5)\n)",
        "f := bucket(2, 5)\ng := f ]",
        "x := 1\nplot(x)",
        "x := 1\ny := x(3)",
        "f := bucket(2, 5)\nplot(f, out=)",
        "f := bucket(2, 5)\nplot(f, nosuch=\"x\")",
        "plot(nosuch)",
        "#!syntax version 9.9",
        "#!syntax version banana"
    ];

    private static IEnumerable<ParseError> ErrorsOf(string programText)
        => Program.FromText(programText).Errors
            .Select(error => error.Source)
            .OfType<ParseError>();

    private static IReadOnlyList<string> MatchersRecognising(ParseError error) => error switch
    {
        ParserError parserError => SyntaxErrorMessages.ParserMatchers
            .Where(matcher => matcher.Recognises(parserError))
            .Select(matcher => matcher.Name)
            .ToList(),
        LexerError lexerError => SyntaxErrorMessages.LexerMatchers
            .Where(matcher => matcher.Recognises(lexerError))
            .Select(matcher => matcher.Name)
            .ToList(),
        _ => []
    };

    /// <summary>
    /// Two matchers recognising one error is a defect: which of them answers would then depend on the order of the registry, and the one that wins is not necessarily the one that understands it.
    /// </summary>
    [Theory]
    [MemberData(nameof(Corpus))]
    public void AtMostOneMatcherRecognisesAnError(string programText)
    {
        foreach (var error in ErrorsOf(programText))
        {
            var claiming = MatchersRecognising(error);

            Assert.True(
                claiming.Count <= 1,
                $"'{programText}' at line {error.Position.Line}:{error.Position.Column} is recognised by "
                    + $"{claiming.Count} matchers: {string.Join(", ", claiming)}");
        }
    }

    /// <summary>
    /// A matcher nothing reaches is one whose guard is wrong, or whose case the parser no longer reports that way.
    /// </summary>
    [Fact]
    public void EveryMatcherRecognisesSomethingInTheCorpus()
    {
        var reached = Corpus
            .SelectMany(row => ErrorsOf(row.Data))
            .SelectMany(MatchersRecognising)
            .ToHashSet();

        var declared = SyntaxErrorMessages.ParserMatchers.Select(matcher => matcher.Name)
            .Concat(SyntaxErrorMessages.LexerMatchers.Select(matcher => matcher.Name));

        Assert.All(declared, name => Assert.Contains(name, reached));
    }

    /// <summary>
    /// The message is printed after the position, so it is a fragment: no capital opening it, no period closing it.
    /// The hint is the sentence.
    /// </summary>
    [Theory]
    [MemberData(nameof(Corpus))]
    public void WhatAMatcherWritesIsAFragment(string programText)
    {
        var written = Program.FromText(programText).Errors.Where(error => error.RewrittenBy is not null);

        Assert.All(written, error =>
        {
            Assert.False(char.IsUpper(error.Message[0]), $"'{error.Message}' opens with a capital");
            Assert.False(error.Message.EndsWith('.'), $"'{error.Message}' closes with a period");
        });
    }

    /// <summary>
    /// A rewritten message names what recognised it, so that a report can be traced back to a matcher, and one that was not rewritten names nothing.
    /// </summary>
    [Fact]
    public void OnlyARewrittenMessageNamesItsMatcher()
    {
        var rewritten = Assert.Single(Program.FromText("g := f + 1").Errors);
        Assert.Equal("unknown variable", rewritten.RewrittenBy);

        var kept = Assert.Single(Program.FromText("f := bucket(2, 5)\n)").Errors);
        Assert.Null(kept.RewrittenBy);
        Assert.StartsWith("no viable alternative at input", kept.Message);
    }
}
