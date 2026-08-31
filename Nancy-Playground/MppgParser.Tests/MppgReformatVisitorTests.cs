using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The reformatted echo: compound operands are parenthesized, call-shaped operators and rational
/// literals are tight, and binary operators stay spaced.
/// </summary>
public class MppgReformatVisitorTests
{
    private const string Declarations = """
        x := 2
        y := 3
        z := 4
        f := bucket(2, 3)
        g := bucket(4, 5)
        h := bucket(6, 7)
        """;

    public static IEnumerable<object[]> ExpressionCommandCases =>
        new (string Input, string Expected)[]
        {
            ("x + y", "x + y"),
            ("x * y", "x * y"),
            ("x * y + z", "(x * y) + z"),
            ("x + y * z", "x + (y * z)"),
            ("f + g", "f + g"),
            ("f * g", "f * g"),
            ("f + 1/2", "f + (1/2)"),
            ("f * 1/2", "(f * 1) / 2"),
            ("1/2 * f", "(1/2) * f"),
            ("f / 1/2", "(f / 1) / 2"),
            ("x * y * f", "(x * y) * f"),
            ("f - x + y", "(f - x) + y"),
            ("f + x * y", "f + (x * y)"),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ExpressionCommandCases))]
    public void ExpressionCommandEchoesGrouping(string input, string expected)
    {
        var program = Program.FromText($"{Declarations}\n{input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(expected, program.Statements[^1].Text);
    }

    [Theory]
    [MemberData(nameof(ExpressionCommandCases))]
    public void AssignmentEchoesGrouping(string input, string expected)
    {
        var program = Program.FromText($"{Declarations}\nresult := {input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal($"result := {expected}", program.Statements[^1].Text);
    }

    [Fact]
    public void ConstructorEchoesCompact()
    {
        var program = Program.FromText("f := bucket(2, 5)");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal("f := bucket(2, 5)", program.Statements[0].Text);
    }

    public static IEnumerable<object[]> FunctionCallCases =>
        new (string Input, string Expected)[]
        {
            ("ratency(1, 3)", "ratency(1, 3)"),
            ("stair(0, y, z)", "stair(0, y, z)"),
            ("delay(x)", "delay(x)"),
            ("subaddclosure(f)", "subaddclosure(f)"),
            ("hShift(f, 2)", "hShift(f, 2)"),
            ("hDev(f, g)", "hDev(f, g)"),
            ("f(x)", "f(x)"),
            ("f(6-)", "f(6-)"),
            ("f(6~+)", "f(6~+)"),
            ("floor(x + y)", "floor(x + y)"),
            ("abs(x)", "abs(x)"),
            ("pow(x, y)", "pow(x, y)"),
            ("affine(x / 2, 0)", "affine(x / 2, 0)"),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FunctionCallCases))]
    public void FunctionCallEchoesCompact(string input, string expected)
    {
        var program = Program.FromText($"{Declarations}\n{input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(expected, program.Statements[^1].Text);
    }

    public static IEnumerable<object[]> NumberLiteralCases =>
        new (string Input, string Expected)[]
        {
            ("3/2", "3/2"),
            ("-3/2", "-3/2"),
            ("-0.25", "-0.25"),
            ("-inf", "-inf"),
            ("-x", "-x"),
            ("2 + -3", "2 + -3"),
            ("x / y", "x / y"),
            ("x / 2", "x / 2"),
            ("1/2 + 1/3", "(1/2) + (1/3)"),
            ("-(x + y)", "-(x + y)"),
            ("(x + y) * z", "(x + y) * z"),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(NumberLiteralCases))]
    public void NumberLiteralEchoesTight(string input, string expected)
    {
        var program = Program.FromText($"{Declarations}\n{input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(expected, program.Statements[^1].Text);
    }

    public static IEnumerable<object[]> CommandCases =>
        new (string Input, string Expected)[]
        {
            ("printExpression ( f )", "printExpression(f)"),
            ("plot ( f , g )", "plot(f, g)"),
            ("plotTikz ( f )", "plotTikz(f)"),
            ("""plot ( f , out = "p.png" )""", """plot(f, out="p.png")"""),
            ("""plot(f, out="p.png", gui="no")""", """plot(f, out="p.png", gui="no")"""),
            ("plot ( f , xlim = [ 0 , 10 ] )", "plot(f, xlim=[0, 10])"),
            ("plot(f, ylim=[1/2, 10])", "plot(f, ylim=[1/2, 10])"),
        }.ToXUnitTestCases();

    /// <summary>
    /// The commands are call-shaped, so they echo as one writes them rather than as spaced tokens.
    /// </summary>
    [Theory]
    [MemberData(nameof(CommandCases))]
    public void CommandEchoesTight(string input, string expected)
    {
        var program = Program.FromText($"{Declarations}\n{input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(expected, program.Statements[^1].Text);
    }

    [Fact]
    public void AssertionEchoesOperandGrouping()
    {
        var program = Program.FromText($"{Declarations}\nassert(f * g * h = f)");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal("assert((f * g) * h = f)", program.Statements[^1].Text);
    }

    public static IEnumerable<object[]> SegmentCases =>
        new (string Input, string Expected)[]
        {
            // the endpoints follow the comma of an argument list, the slope is spaced, the brackets are tight
            ("uaf( [(0,-3)1(1,-2)[ [(1,-2)0(+inf,-2)[ )", "uaf([(0, -3) 1 (1, -2)[ [(1, -2) 0 (+inf, -2)[)"),
            // a spot, and a segment whose slope is left to be computed
            ("uaf( [(0,0)] ](0,0)(1,1)[ [(1,1)0(+inf,1)[ )", "uaf([(0, 0)] ](0, 0) (1, 1)[ [(1, 1) 0 (+inf, 1)[)"),
            // every bracket combination
            ("uaf( [(0,0)1(1,1)] ](1,1)0(+inf,1)[ )", "uaf([(0, 0) 1 (1, 1)] ](1, 1) 0 (+inf, 1)[)"),
            // the transient part, the period and the increment are arguments of upp
            ("upp( period( [(0,0)0(2,0)[ ), 1/2, 1)", "upp(period([(0, 0) 0 (2, 0)[), 1/2, 1)"),
        }.ToXUnitTestCases();

    /// <summary>
    /// A segment reads as a bracketed pair of endpoints with the slope between them.
    /// </summary>
    [Theory]
    [MemberData(nameof(SegmentCases))]
    public void SegmentEchoesNormalized(string input, string expected)
    {
        var program = Program.FromText($"c := {input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal($"c := {expected}", program.Statements[^1].Text);
    }

    /// <summary>
    /// A property assertion echoes as written, the comparison form and the negated form alike.
    /// </summary>
    [Theory]
    [InlineData("assert(f is subadditive)")]
    [InlineData("assert(f is not subadditive)")]
    [InlineData("assert(x is integer)")]
    [InlineData("assert(x is not zero)")]
    public void PropertyAssertionEchoesAsWritten(string input)
    {
        var program = Program.FromText($"{Declarations}\nx := 1\n{input}");

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(input, program.Statements[^1].Text);
    }
}
