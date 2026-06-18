using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class RationalParsing
{
    private readonly ITestOutputHelper _testOutputHelper;

    // test variables
    private static readonly Rational XValue = new(2);
    private static readonly Rational YValue = new(3);

    private static State StateWithNumberVariables() =>
        new(
            [
                ("x", Expressions.Expressions.FromRational(XValue, "x")),
                ("y", Expressions.Expressions.FromRational(YValue, "y")),
            ]
        );

    public RationalParsing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<(string mppg, Rational expected)> KnownMppgRationalPairs =
    [
        ( "0", new Rational(0) ),
        ( "1", new Rational(1) ),
        ( "-3", new Rational(-3) ),
        ( "3/2", new Rational(3, 2) ),
        ( "-3/2", new Rational(-3, 2) ),
        ( "0.25", new Rational(1, 4) ),
        ( "-0.25", new Rational(-1, 4) ),
        ( "3200/0.00025", new Rational(12800000) ),
        ( "+inf", Rational.PlusInfinity ),
        ( "-inf", Rational.MinusInfinity ),
        ( "+infinity", Rational.PlusInfinity ),
        ( "-infinity", Rational.MinusInfinity ),
    ];

    public static IEnumerable<object[]> KnownMppgRationalTestCases =>
        KnownMppgRationalPairs.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(KnownMppgRationalTestCases))]
    public void MppgRationalParsingEquivalence(string mppg, Rational expected)
    {
        var state = new State();
        var ie = ExpressionParsing.Parse(mppg, state);
        Assert.IsAssignableFrom<RationalExpression>(ie);
        var curve = ((RationalExpression)ie).Value;
        Assert.Equal(expected, curve);
    }

    public static List<(string mppg, Rational expected)> AmbiguousVariableRationalExpressionPairs =
    [
        ( "x * y", new Rational(6) ),
        ( "x / y", new Rational(2, 3) ),
        ( "x + y", new Rational(5) ),
        ( "x - y", new Rational(-1) ),
        ( "x /\\ y", XValue ),
        ( "x \\/ y", YValue ),
        ( "-x", new Rational(-2) ),
    ];

    public static IEnumerable<object[]> AmbiguousVariableRationalExpressionTestCases =>
        AmbiguousVariableRationalExpressionPairs.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(AmbiguousVariableRationalExpressionTestCases))]
    public void AmbiguousVariableRationalExpressionsParseToExpectedResult(
        string mppg,
        Rational expected)
    {
        var ie = ExpressionParsing.Parse(mppg, StateWithNumberVariables());
        var expression = Assert.IsAssignableFrom<RationalExpression>(ie);

        Assert.Equal(expected, expression.Compute());
    }

    private static State StateWithFunctionAndNumberVariable() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(10, 5), "f")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(new Rational(3), "x")),
            ]
        );

    public static IEnumerable<object[]> FunctionValueAtTestCases =>
        new List<(string mppg, Rational expected)>
        {
            ("f(0)", new Rational(0)),
            ("f(5)", new Rational(0)),
            ("f(10)", new Rational(50)),
            ("f(20)", new Rational(150)),
            ("f((10))", new Rational(50)),
            ("f(+(5))", new Rational(0)),
            ("f(+(0))", new Rational(0)),
            ("+f(10)", new Rational(50)),
            ("f(f(10))", new Rational(450)),
            ("f(f(0))", new Rational(0)),
            ("f(x)", new Rational(0)),
            ("f(10 + x)", new Rational(80)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FunctionValueAtTestCases))]
    public void FunctionValueAtParsesToExpectedResult(
        string mppg,
        Rational expected)
    {
        var ie = ExpressionParsing.Parse(mppg, StateWithFunctionAndNumberVariable());
        var expression = Assert.IsAssignableFrom<RationalExpression>(ie);

        Assert.Equal(expected, expression.Compute());
    }
}
