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
        ( "floor(7/2)", new Rational(3) ),
        ( "ceil(7/2)", new Rational(4) ),
        ( "floor(-7/2)", new Rational(-4) ),
        ( "ceil(-7/2)", new Rational(-3) ),
        ( "floor(3)", new Rational(3) ),
        ( "ceil(3)", new Rational(3) ),
        ( "floor(0.25)", new Rational(0) ),
        ( "ceil(0.25)", new Rational(1) ),
        ( "floor(ceil(7/2))", new Rational(4) ),
        ( "abs(-7/2)", new Rational(7, 2) ),
        ( "abs(7/2)", new Rational(7, 2) ),
        ( "abs(0)", new Rational(0) ),
        ( "abs(-inf)", Rational.PlusInfinity ),
        ( "pow(2, 10)", new Rational(1024) ),
        ( "pow(2, -2)", new Rational(1, 4) ),
        ( "pow(-7/2, 3)", new Rational(-343, 8) ),
        ( "pow(5, 0)", new Rational(1) ),
        // the remainder takes the sign of the dividend
        ( "7 mod 3", new Rational(1) ),
        ( "-7 mod 3", new Rational(-1) ),
        ( "-7/2 mod 3", new Rational(-1, 2) ),
        ( "gcd(12, 18)", new Rational(6) ),
        ( "lcm(4, 6)", new Rational(12) ),
        // and these are defined on rationals, not only on integers
        ( "gcd(1/2, 1/3)", new Rational(1, 6) ),
        ( "lcm(1/2, 1/3)", new Rational(1) ),
        // nesting, and the operators of the same version composing with each other
        ( "abs(-7 mod 3)", new Rational(1) ),
        ( "gcd(lcm(4, 6), 18)", new Rational(6) ),
        ( "pow(abs(-2), 3)", new Rational(8) ),
        ( "floor(pow(3, 2) / 2)", new Rational(4) ),
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
        ( "floor(x / y)", new Rational(0) ),
        ( "ceil(x / y)", new Rational(1) ),
        ( "floor(-x / y)", new Rational(-1) ),
        ( "ceil(-x / y)", new Rational(0) ),
        ( "floor(x) + y", new Rational(5) ),
        // floor of a scalar stays a scalar, so the division around it is not integer division
        ( "floor(7/2) / 2", new Rational(3, 2) ),
        ( "ceil(7/2) / 4", new Rational(1) ),
        // both operands round to integers, yet the division between them is not an integer division
        ( "floor(7/2) / floor(9/2)", new Rational(3, 4) ),
        ( "abs(x - y)", new Rational(1) ),
        ( "pow(x, y)", new Rational(8) ),
        ( "y mod x", new Rational(1) ),
        // mod binds like the other product operators, and folds left to right with them
        ( "y mod x + 1", new Rational(2) ),
        ( "1 + y mod x", new Rational(2) ),
        ( "x * y mod 2", new Rational(0) ),
        ( "gcd(x, y) * lcm(x, y)", new Rational(6) ),
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

    // The exponent is truncated to an integer by the operation itself, which would silently give the
    // power of a different exponent: the syntax rejects it instead.
    [Theory]
    [InlineData("pow(2, 1/2)")]
    [InlineData("pow(4, 0.5)")]
    [InlineData("pow(2, -3/2)")]
    [InlineData("pow(2, inf)")]
    public void PowRejectsANonIntegerExponent(string mppg)
    {
        var exception = Assert.ThrowsAny<Exception>(
            () => ExpressionParsing.Parse(mppg, new State()));

        Assert.Contains("exponent of pow must be an integer", exception.Message);
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
