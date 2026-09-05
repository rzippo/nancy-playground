using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class CurveParsing
{
    private readonly ITestOutputHelper _testOutputHelper;
    
    // test variables
    private static readonly Rational XValue = new(2);
    
    private static Curve BucketFunction() =>
        new SigmaRhoArrivalCurve(3, 2);

    private static Curve ServiceCurve() =>
        new RateLatencyServiceCurve(1, 1);

    private static Curve ConstantFunction(Rational value) =>
        new(
            new Sequence([
                new Point(0, value),
                Segment.Constant(0, 1, value),
            ]),
            0,
            1,
            0
        );

    private static Curve AffineFunction(Rational slope, Rational constant) =>
        new(
            new Sequence([
                new Point(0, constant),
                new Segment(0, 1, constant, slope),
            ]),
            0,
            1,
            slope
        );

    private static State StateWithVariables() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(BucketFunction(), "f")),
                ("g", Expressions.Expressions.FromCurve(ServiceCurve(), "g")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(XValue, "x")),
            ]
        );

    public CurveParsing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<(string mppg, Curve expected)> KnownMppgCurvePairs =
    [
        (
            "ratency(1, 2)",
            new RateLatencyServiceCurve(1, 2)
        ),
        (
            "ratency(1, 0)",
            new RateLatencyServiceCurve(1, 0)
        ),
        (
            "ratency(0, 2)",
            new RateLatencyServiceCurve(0, 2)
        ),
        (
            "ratency(0, 0)",
            new RateLatencyServiceCurve(0, 0)
        ),
        (
            "ratency(0, 0)",
            Curve.Zero()
        ),
        (
            "bucket(2, 3)",
            new SigmaRhoArrivalCurve(3, 2)
        ),
        (
            "affine(2, 3)",
            new Curve(
                new Sequence([
                    new Point(0, 3),
                    new Segment(0, 1, 3, 2)
                ]),
                0, 1, 2
            )
        ),
        (
            "affine(2, 0)",
            new Curve(
                new Sequence([
                    new Point(0, 0),
                    new Segment(0, 1, 0, 2)
                ]),
                0, 1, 2
            )
        ),
        (
            "affine(0, 2)",
            new Curve(
                new Sequence([
                    new Point(0, 2),
                    new Segment(0, 1, 2, 0)
                ]),
                0, 1, 0
            )
        ),
        (
            "step(5, 10)",
            new StepCurve(10, 5)
        ),
        (
            "step(0, 10)",
            new StepCurve(10, 0)
        ),
        (
            "stair(2, 3, 4)",
            new StairCurve(4, 3).DelayBy(2)
        ),
        (
            "stair(0, 3, 4)",
            new StairCurve(4, 3)
        ),
        // todo: constant functions are implicitly constructed, how to test them?
        (
            "delay(7)",
            new DelayServiceCurve(7)
        ),
        (
            "delay(0)",
            new DelayServiceCurve(0)
        ),
        (
            "zero",
            Curve.Zero()
        ),
        (
            "epsilon",
            Curve.PlusInfinite()
        ),
        (
            "upp( period ( [(0, 0) 0 (2, 0)[ [(2, 0) 1 (7, 5)] ](7, 5) 0 ( 12, 5 )[ ))",
            new Curve(
                new Sequence([
                    Point.Origin(),
                    Segment.Zero(0, 2),
                    Point.Zero(2),
                    new Segment(2, 7, 0, 1),
                    new Point(7, 5),
                    Segment.Constant(7, 12, 5)
                ]),
                0,
                12,
                5
            )
        ),
        (
            "uaf ( [ ( 0 , 0 ) 0 ( 0 , 0 ) ] ] ( 0 , 0 ) - 1 ( 1 , - 1 ) ] ] ( 1 , 0 ) - 1 ( 2 , - 1 ) ] ] ( 2 , - 1 ) 1 ( 3 , 0 ) ] ] ( 3 , 0 ) 0 ( 4 , 0 ) ] ] ( 4 , 0 ) 1 ( 5 , 1 ) ] ] ( 5 , 1 ) - 1 ( 7 , - 1 ) ] ] ( 7 , - 1 ) 1 ( + Infinity , + Infinity ) [ )",
            new Curve(
                new Sequence([
                    Point.Origin(),
                    new Segment(0, 1, 0, -1),
                    new Point(1, -1),
                    new Segment(1, 2, 0, -1),
                    new Point(2, -1),
                    new Segment(2, 3, -1, 1),
                    new Point(3, 0),
                    new Segment(3, 4, 0, 0),
                    new Point(4, 0),
                    new Segment(4, 5, 0, 1),
                    new Point(5, 1),
                    new Segment(5, 7, 1, -1),
                    new Point(7, -1),
                    new Segment(7, 8, -1, 1)
                ]),
                7, 1, 1
            )
        ),
        (
            "uaf( [(0,0)1(+inf,+inf)[ )",
            new Curve(
                new Sequence([
                    new Point(0, 0),
                    new Segment(0, 1, 0, 1)
                ]),
                0, 1, 1
            )
        ),
        (
            "ratency(1, +inf)",
            Curve.PlusInfinite()
        ),
        (
            "step(1, +inf)",
            new StepCurve(Rational.PlusInfinity, 1)
        )
    ];

    public static IEnumerable<object[]> KnownMppgCurveTestCases =>
        KnownMppgCurvePairs.ToXUnitTestCases();
    
    [Theory]
    [MemberData(nameof(KnownMppgCurveTestCases))]
    public void MppgCurveParsingEquivalence(string mppg, Curve expected)
    {
        var state = new State();
        var ie = ExpressionParsing.Parse(mppg, state);
        Assert.IsAssignableFrom<CurveExpression>(ie);
        var curve = ((CurveExpression)ie).Value;
        Assert.True(Curve.Equivalent(expected, curve));
    }

    [Fact]
    public void ExplicitCurveSegmentsMayUseNumberExpressions()
    {
        var state = new State(
            [
                ("z", Expressions.Expressions.FromRational(0, "z")),
                ("lmax", Expressions.Expressions.FromRational(20, "lmax")),
                ("C", Expressions.Expressions.FromRational(100, "C")),
                ("idSlA", Expressions.Expressions.FromRational(65, "idSlA")),
                ("y", Expressions.Expressions.FromRational(new Rational(1, 5), "y")),
            ]);

        var actual = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse("uaf( [(z,z)z(y,z)]](y,z)idSlA(+inf,+inf)])", state));

        Assert.Equal(65, actual.Value.PseudoPeriodSlope);
    }

    [Fact]
    public void UppPeriodMayUseInfiniteRightEndpointForUltimatelyConstantTail()
    {
        var actual = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse("upp([(0, 0)0(2.75,0)] ](2.75,0.5)0(3, 0.5)] ](3,1.5)0(5.5, 1.5)] ](5.5, 3)0(7,3)], period(](7, 3)0(+Infinity, 3)]))", new State()));

        var value = actual.ValueAt(100).Compute();

        Assert.Equal(3, value);
    }

    public static IEnumerable<object[]> MixedScalarCurveOperatorTestCases =>
        new List<(string mppg, Curve expected)>
        {
            ("f * 5", BucketFunction().Scale(new Rational(5))),
            ("5 * f", BucketFunction().Scale(new Rational(5))),
            ("f / 5", BucketFunction().Scale(new Rational(1, 5))),
            // a scalar over a curve is the deconvolution of the constant it stands for
            ("5 / f", ConstantFunction(new Rational(5)).Deconvolution(BucketFunction())),
            // composing two constants reads the left one wherever it is read
            ("5 comp 3", ConstantFunction(new Rational(5))),
            ("f + 5", BucketFunction().VerticalShift(new Rational(5))),
            ("5 + f", BucketFunction().VerticalShift(new Rational(5))),
            ("f - 5", BucketFunction().VerticalShift(new Rational(-5))),
            ("5 - f", BucketFunction().Negate().VerticalShift(new Rational(5))),
            ("f /\\ 5", BucketFunction().Minimum(ConstantFunction(new Rational(5)))),
            ("5 /\\ f", BucketFunction().Minimum(ConstantFunction(new Rational(5)))),
            ("f \\/ 5", BucketFunction().Maximum(ConstantFunction(new Rational(5)))),
            ("5 \\/ f", BucketFunction().Maximum(ConstantFunction(new Rational(5)))),
            ("f * x", BucketFunction().Scale(XValue)),
            ("x * f", BucketFunction().Scale(XValue)),
            ("f / x", BucketFunction().Scale(new Rational(1, 2))),
            ("f * (x)", BucketFunction().Scale(XValue)),
            ("(x) * f", BucketFunction().Scale(XValue)),
            ("f / (x)", BucketFunction().Scale(new Rational(1, 2))),
            ("f + x", BucketFunction().VerticalShift(XValue)),
            ("x + f", BucketFunction().VerticalShift(XValue)),
            ("f - x", BucketFunction().VerticalShift(new Rational(-2))),
            ("x - f", BucketFunction().Negate().VerticalShift(XValue)),
            ("f + (x)", BucketFunction().VerticalShift(XValue)),
            ("f - (x)", BucketFunction().VerticalShift(new Rational(-2))),
            ("f /\\ x", BucketFunction().Minimum(ConstantFunction(XValue))),
            ("x /\\ f", BucketFunction().Minimum(ConstantFunction(XValue))),
            ("f \\/ x", BucketFunction().Maximum(ConstantFunction(XValue))),
            ("x \\/ f", BucketFunction().Maximum(ConstantFunction(XValue))),
            ("f /\\ (x)", BucketFunction().Minimum(ConstantFunction(XValue))),
            ("f \\/ (x)", BucketFunction().Maximum(ConstantFunction(XValue))),
            ("f + (5)", BucketFunction().VerticalShift(new Rational(5))),
            ("(5) + f", BucketFunction().VerticalShift(new Rational(5))),
            ("+f", BucketFunction()),
            ("-f", BucketFunction().Negate()),
            ("f + +5", BucketFunction().VerticalShift(new Rational(5))),
            ("f + -3", BucketFunction().VerticalShift(new Rational(-3))),
            ("g + (5)", ServiceCurve().VerticalShift(new Rational(5))),
            ("(5) + g", ServiceCurve().VerticalShift(new Rational(5))),
            ("f(x) * g", ServiceCurve().Scale(new Rational(7))),
            ("g * f(x)", ServiceCurve().Scale(new Rational(7))),
            ("f(x) + g", ServiceCurve().VerticalShift(new Rational(7))),
            ("g + f(x)", ServiceCurve().VerticalShift(new Rational(7))),
            ("f(x) - g", ServiceCurve().Negate().VerticalShift(new Rational(7))),
            ("g - f(x)", ServiceCurve().VerticalShift(new Rational(-7))),
            ("f(x) /\\ g", ServiceCurve().Minimum(ConstantFunction(new Rational(7)))),
            ("g /\\ f(x)", ServiceCurve().Minimum(ConstantFunction(new Rational(7)))),
            ("f(x) \\/ g", ServiceCurve().Maximum(ConstantFunction(new Rational(7)))),
            ("g \\/ f(x)", ServiceCurve().Maximum(ConstantFunction(new Rational(7)))),
            ("f(x) comp g", ConstantFunction(new Rational(7))),
            ("g comp f(x)", ConstantFunction(new Rational(6))),
            ("f(x) * g + x", ServiceCurve().Scale(new Rational(7)).VerticalShift(XValue)),
            ("f(x) + g * x", ServiceCurve().Scale(XValue).VerticalShift(new Rational(7))),
            ("x comp f", ConstantFunction(XValue)),
            ("f comp x", ConstantFunction(new Rational(7))),
            ("f + x comp g", BucketFunction().Addition(ConstantFunction(XValue))),
            // a floor or ceil of a scalar is a scalar wherever it appears, so these scale and shift
            ("floor(x) * f", BucketFunction().Scale(XValue)),
            ("f * ceil(x / 3)", BucketFunction().Scale(new Rational(1))),
            ("f / floor(x)", BucketFunction().Scale(new Rational(1, 2))),
            ("floor(f(x)) + g", ServiceCurve().VerticalShift(new Rational(7))),
            ("g + ceil(f(x) / 2)", ServiceCurve().VerticalShift(new Rational(4))),
            // the scalar operators scale and shift, as any other scalar does
            ("abs(-3) * f", BucketFunction().Scale(new Rational(3))),
            ("f * abs(0 - x)", BucketFunction().Scale(XValue)),
            ("f / gcd(4, 6)", BucketFunction().Scale(new Rational(1, 2))),
            ("f * pow(x, 3)", BucketFunction().Scale(new Rational(8))),
            ("f + (7 mod 3)", BucketFunction().VerticalShift(new Rational(1))),
            ("lcm(2, 3) + f", BucketFunction().VerticalShift(new Rational(6))),
            ("f * abs(f(x))", BucketFunction().Scale(new Rational(7))),
            // a fraction is the scalar operand of a sum operator, unparenthesised
            ("f + 1/2", BucketFunction().VerticalShift(new Rational(1, 2))),
            ("1/2 + f", BucketFunction().VerticalShift(new Rational(1, 2))),
            ("f - 1/2", BucketFunction().VerticalShift(new Rational(-1, 2))),
            ("1/2 - f", BucketFunction().Negate().VerticalShift(new Rational(1, 2))),
            ("f /\\ 1/2", BucketFunction().Minimum(ConstantFunction(new Rational(1, 2)))),
            ("f \\/ 1/2", BucketFunction().Maximum(ConstantFunction(new Rational(1, 2)))),
            ("f + x / 3", BucketFunction().VerticalShift(new Rational(2, 3))),
            // a division chain folds left-to-right, as it does in a plain number expression
            ("f + 1/2/3", BucketFunction().VerticalShift(new Rational(1, 6))),
            // a fraction is also the scalar operand on the left of the product operators
            ("1/2 * f", BucketFunction().Scale(new Rational(1, 2))),
            ("1/2 * f + g", BucketFunction().Scale(new Rational(1, 2)).Addition(ServiceCurve())),
            ("f + 1/2 * g", BucketFunction().Addition(ServiceCurve().Scale(new Rational(1, 2)))),
            ("x/2 * f", BucketFunction().Scale(XValue / 2)),
            ("x * x * f", BucketFunction().Scale(XValue * XValue)),
            // The scalar operand carries its sign, whichever operator it belongs to.
            // A sign in front of something no literal can spell reads the same as one in front of a literal.
            ("f + -x", BucketFunction().VerticalShift(-XValue)),
            ("f - -x", BucketFunction().VerticalShift(XValue)),
            ("f + +x", BucketFunction().VerticalShift(XValue)),
            ("f * +x", BucketFunction().Scale(XValue)),
            ("f * -(-x)", BucketFunction().Scale(XValue)),
            ("f / -(-x)", BucketFunction().Scale(new Rational(1, 2))),
            ("-(-x) * f", BucketFunction().Scale(XValue)),
            ("+x * f", BucketFunction().Scale(XValue)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(MixedScalarCurveOperatorTestCases))]
    public void MixedScalarCurveOperatorsParseToExpectedResult(
        string mppg,
        Curve expected)
    {
        var state = StateWithVariables();

        var actual = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(mppg, state));

        Assert.True(Curve.Equivalent(expected, actual.Compute()));
    }

    public static IEnumerable<object[]> FloorCeilCurveValueTestCases =>
        new List<(string mppg, Rational time, Rational expected)>
        {
            // g is ratency(1, 1), i.e. g(t) = max(0, t - 1)
            ("floor(g)", new Rational(7, 2), new Rational(2)),
            ("ceil(g)", new Rational(7, 2), new Rational(3)),
            ("floor(g)", new Rational(3), new Rational(2)),
            ("ceil(g)", new Rational(3), new Rational(2)),
            ("floor(g)", new Rational(1, 2), new Rational(0)),
            ("ceil(g)", new Rational(1, 2), new Rational(0)),
            // a negated curve rounds away from zero on the floor side, as the values are negative
            ("floor(-g)", new Rational(7, 2), new Rational(-3)),
            ("ceil(-g)", new Rational(7, 2), new Rational(-2)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FloorCeilCurveValueTestCases))]
    public void FloorCeilOfACurveRoundsItsValues(string mppg, Rational time, Rational expected)
    {
        var curve = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(mppg, StateWithVariables()));

        Assert.Equal(expected, curve.ValueAt(time).Compute());
    }

    // Before the operators existed, scripts spelled the floor of a curve as a composition with
    // right-ext(stair(1, 1, 1)) and its ceiling as one with stair(0, 1, 1), as the .nc files of
    // NC-files still do: the operators must compute the same curves.
    public static IEnumerable<object[]> FloorCeilCompositionEquivalenceTestCases =>
        new List<(string mppg, string composition)>
        {
            ("floor(f)", "right-ext(stair(1, 1, 1)) comp f"),
            ("ceil(f)", "stair(0, 1, 1) comp f"),
            ("floor(g)", "right-ext(stair(1, 1, 1)) comp g"),
            ("ceil(g)", "stair(0, 1, 1) comp g"),
            // the release count of a task activated every 2 units of another's output, as in the
            // hal-04513292v1 example, times the work each release brings
            ("floor(f / 2) * 4", "(right-ext(stair(1, 1, 1)) comp (f / 2)) * 4"),
            // a packetizer, which lets through whole packets of size 5 only
            ("floor(f / 5) * 5", "(right-ext(stair(1, 1, 1)) comp (f / 5)) * 5"),
            // the number of packets of size 5 needed to carry f
            ("ceil(f / 5)", "stair(0, 1, 1) comp (f / 5)"),
            // the arrival curve of a periodic task of period 4 and workload 3 per release
            ("ceil(affine(1/4, 0)) * 3", "(stair(0, 1, 1) comp affine(1/4, 0)) * 3"),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FloorCeilCompositionEquivalenceTestCases))]
    public void FloorCeilMatchTheCompositionsTheyReplace(string mppg, string composition)
    {
        var state = StateWithVariables();

        var withOperator = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(mppg, state));
        var withComposition = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(composition, state));

        Assert.True(Curve.Equivalent(withComposition.Compute(), withOperator.Compute()));
    }

    // The staircase constructor is the arrival curve of a periodic task, which the ceiling of a line
    // states directly: o is the first release, l the period and h the workload of a release.
    [Theory]
    [InlineData(4, 3)]
    [InlineData(60, 35)]
    [InlineData(5, 2)]
    public void StaircaseIsTheCeilingOfALine(int period, int height)
    {
        var state = StateWithVariables();

        var staircase = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse($"stair(0, {period}, {height})", state));
        var ceiling = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse($"ceil(affine(1/{period}, 0)) * {height}", state));

        Assert.True(Curve.Equivalent(staircase.Compute(), ceiling.Compute()));
    }

    // A packetizer outputs no more than its input, and lags by less than one packet.
    [Theory]
    [InlineData(5)]
    [InlineData(2)]
    public void PacketizerOfACurveStaysWithinOnePacketOfIt(int packetSize)
    {
        var state = StateWithVariables();
        var input = BucketFunction();

        var packetized = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse($"floor(f / {packetSize}) * {packetSize}", state)).Compute();

        Assert.True(packetized <= input);
        Assert.True(input <= packetized.VerticalShift(packetSize));
    }

    [Fact]
    public void FunctionScalingParsesEquivalentlyWithScalarOnEitherSide()
    {
        var state = StateWithVariables();
        var expected = BucketFunction().Scale(new Rational(3));

        var leftScalar = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse("3 * f", state));
        var rightScalar = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse("f * 3", state));

        Assert.True(Curve.Equivalent(expected, leftScalar.Compute()));
        Assert.True(Curve.Equivalent(expected, rightScalar.Compute()));
        Assert.True(Curve.Equivalent(leftScalar.Compute(), rightScalar.Compute()));
    }

    public static IEnumerable<object[]> AmbiguousVariableCurveOperatorTestCases =>
        new List<(string mppg, Curve expected)>
        {
            ("f * g", BucketFunction().Convolution(ServiceCurve())),
            ("f / g", BucketFunction().Deconvolution(ServiceCurve())),
            ("f + g", BucketFunction().Addition(ServiceCurve())),
            ("f - g", BucketFunction().Subtraction(ServiceCurve())),
            ("f /\\ g", BucketFunction().Minimum(ServiceCurve())),
            ("f \\/ g", BucketFunction().Maximum(ServiceCurve())),
            ("f *^ g", BucketFunction().MaxPlusConvolution(ServiceCurve())),
            ("f /^ g", BucketFunction().MaxPlusDeconvolution(ServiceCurve())),
            ("f comp g", BucketFunction().Composition(ServiceCurve())),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(AmbiguousVariableCurveOperatorTestCases))]
    public void AmbiguousVariableCurveOperatorsParseToExpectedResult(
        string mppg,
        Curve expected)
    {
        var state = StateWithVariables();

        var actual = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(mppg, state));

        Assert.True(Curve.Equivalent(expected, actual.Compute()));
    }

    private static State StateWithCurveAndTwoNumberVars() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(10, 5), "f")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(new Rational(3), "x")),
                ("y", Expressions.Expressions.FromRational(new Rational(4), "y")),
            ]
        );

    public static IEnumerable<object[]> CurveAndTwoNumberVarSampleTestCases =>
        new List<(string mppg, Rational observation, Rational expected)>
        {
            ("f + (x + y)", new Rational(10), new Rational(57)),
            ("f - (x - y)", new Rational(10), new Rational(51)),
            ("f * (x * y)", new Rational(10), new Rational(600)),
            ("(x * y) * f", new Rational(10), new Rational(600)),
            ("f / (x / y)", new Rational(10), new Rational(200, 3)),
            ("f /\\ (x /\\ y)", new Rational(10), new Rational(3)),
            ("f \\/ (x \\/ y)", new Rational(10), new Rational(50)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(CurveAndTwoNumberVarSampleTestCases))]
    public void CurveAndTwoNumberVarExpressionsEvaluateToExpectedSample(
        string mppg,
        Rational observation,
        Rational expected)
    {
        var state = StateWithCurveAndTwoNumberVars();
        var expr = ExpressionParsing.Parse(mppg, state);
        var curve = Assert.IsAssignableFrom<CurveExpression>(expr);
        var actual = curve.ValueAt(observation).Compute();

        Assert.Equal(expected, actual);
    }

    private static State StateWithTwoCurvesAndThreeNumberVars() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(new RateLatencyServiceCurve(10, 5), "f")),
                ("g", Expressions.Expressions.FromCurve(ServiceCurve(), "g")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(new Rational(3), "x")),
                ("y", Expressions.Expressions.FromRational(new Rational(4), "y")),
                ("z", Expressions.Expressions.FromRational(new Rational(5), "z")),
            ]
        );

    private static State StateWithPrecedenceVariables() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(BucketFunction(), "f")),
                ("g", Expressions.Expressions.FromCurve(ServiceCurve(), "g")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(new Rational(2), "x")),
                ("y", Expressions.Expressions.FromRational(new Rational(3), "y")),
                ("z", Expressions.Expressions.FromRational(new Rational(4), "z")),
            ]
        );

    private static State StateWithReferencePrecedenceVariables() =>
        new(
            [
                ("f", Expressions.Expressions.FromCurve(AffineFunction(new Rational(1), new Rational(1)), "f")),
                ("g", Expressions.Expressions.FromCurve(AffineFunction(new Rational(2), new Rational(0)), "g")),
                ("h", Expressions.Expressions.FromCurve(AffineFunction(new Rational(3), new Rational(0)), "h")),
            ],
            [
                ("x", Expressions.Expressions.FromRational(new Rational(2), "x")),
                ("y", Expressions.Expressions.FromRational(new Rational(3), "y")),
            ]
        );

    public static IEnumerable<object[]> MixedOperatorPrecedenceTestCases =>
        new List<(string expression, string equivalent, string different, Rational observation)>
        {
            ("x + f * y", "x + (f * y)", "(x + f) * y", new Rational(10)),
            ("f * x + g", "(f * x) + g", "f * (x + g)", new Rational(10)),
            ("f + x * g", "f + (x * g)", "(f + x) * g", new Rational(10)),
            ("f + x comp g", "f + (x comp g)", "(f + x) comp g", new Rational(10)),
            ("f - x comp g", "f - (x comp g)", "(f - x) comp g", new Rational(10)),
            ("f comp x + g", "(f comp x) + g", "f comp (x + g)", new Rational(10)),
            ("x comp f + g", "(x comp f) + g", "x comp (f + g)", new Rational(10)),
            ("f comp g * x", "(f comp g) * x", "f comp (g * x)", new Rational(10)),
            ("x comp g * y", "(x comp g) * y", "x comp (g * y)", new Rational(10)),
            ("f comp x * g", "(f comp x) * g", "f comp (x * g)", new Rational(10)),
            ("f * x comp g", "(f * x) comp g", "f * (x comp g)", new Rational(10)),
            ("f + x /\\ y", "(f + x) /\\ y", "f + (x /\\ y)", new Rational(10)),
            ("x + f /\\ y", "(x + f) /\\ y", "x + (f /\\ y)", new Rational(10)),
            ("f - x \\/ y", "(f - x) \\/ y", "f - (x \\/ y)", new Rational(10)),
            ("x - f + y", "(x - f) + y", "x - (f + y)", new Rational(10)),
            ("x /\\ f \\/ y", "(x /\\ f) \\/ y", "x /\\ (f \\/ y)", new Rational(10)),
            ("f - x /\\ g", "(f - x) /\\ g", "f - (x /\\ g)", new Rational(10)),
            ("x /\\ f - y", "(x /\\ f) - y", "x /\\ (f - y)", new Rational(10)),
            ("f \\/ x + g", "(f \\/ x) + g", "f \\/ (x + g)", new Rational(10)),
            ("f(x) + g * y", "f(x) + (g * y)", "(f(x) + g) * y", new Rational(10)),
            ("f(x) comp g + y", "(f(x) comp g) + y", "f(x) comp (g + y)", new Rational(10)),
            // a fraction is a single operand of a sum operator, not the denominator of a larger fraction
            ("f + 1/2", "f + (1/2)", "(f + 1) / 2", new Rational(10)),
            ("f - 1/2", "f - (1/2)", "(f - 1) / 2", new Rational(10)),
            ("f + 3/2", "f + (3/2)", "(f + 3) / 2", new Rational(10)),
            // the product tier is absorbed into the scalar operand, the sum tier is not
            ("f - x + y", "(f - x) + y", "f - (x + y)", new Rational(10)),
            ("f - x - y", "(f - x) - y", "f - (x - y)", new Rational(10)),
            ("f - x * y", "f - (x * y)", "(f - x) * y", new Rational(10)),
            ("f + x * y", "f + (x * y)", "(f + x) * y", new Rational(10)),
            ("f + x / y", "f + (x / y)", "(f + x) / y", new Rational(10)),
            ("f - x / y", "f - (x / y)", "(f - x) / y", new Rational(10)),
            ("f + x / y * z", "f + (x / y * z)", "f + x / (y * z)", new Rational(10)),
            // Scalar division folds left; ScalarDivisionDivergenceTestCases owns that case.
            // The scalar on the left of a product binds the whole scalar chain before the function takes over, so x * y * f is (x * y) * f.
            // The value cases cover x/2 * f, whose only other grouping, x / (2 * f), is not an expression at all.
            ("x * y * f", "(x * y) * f", "x * (y + f)", new Rational(10)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(MixedOperatorPrecedenceTestCases))]
    public void MixedOperatorPrecedenceMatchesExpectedParenthesizedForm(
        string expression,
        string equivalent,
        string different,
        Rational observation)
    {
        var state = StateWithPrecedenceVariables();
        var actual = ParseCurveValueAt(expression, state, observation);
        var expected = ParseCurveValueAt(equivalent, state, observation);
        var unexpected = ParseCurveValueAt(different, state, observation);

        Assert.Equal(expected, actual);
        Assert.NotEqual(unexpected, actual);
    }

    /// <summary>
    /// Where the two tools differ, middle column ours and right column RTaW's.
    /// Changing one is a change of decision, not a bug fix: see "Scalar operands of the mixed operators" in docs/syntax.md.
    /// </summary>
    public static IEnumerable<object[]> ScalarDivisionDivergenceTestCases =>
        new List<(string expression, string ours, string rtaw, Rational observation)>
        {
            ("f / 1/2", "(f / 1) / 2", "f / (1/2)", new Rational(10)),
            ("f / 1/2/3", "((f / 1) / 2) / 3", "f / ((1/2) / 3)", new Rational(10)),
            ("f / 0.5/2", "(f / 0.5) / 2", "f / (0.5 / 2)", new Rational(10)),
            ("f / 2/0.25", "(f / 2) / 0.25", "f / (2 / 0.25)", new Rational(10)),
            ("f / 1/y", "(f / 1) / y", "f / (1 / y)", new Rational(10)),
            ("f / 1 * y", "(f / 1) * y", "f / (1 * y)", new Rational(10)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ScalarDivisionDivergenceTestCases))]
    public void ScalarDivisionFoldsLeftWhereRTaWReadsTheDivisorWhole(
        string expression,
        string ours,
        string rtaw,
        Rational observation)
    {
        var state = StateWithPrecedenceVariables();
        var actual = ParseCurveValueAt(expression, state, observation);

        Assert.Equal(ParseCurveValueAt(ours, state, observation), actual);
        Assert.NotEqual(ParseCurveValueAt(rtaw, state, observation), actual);
    }

    /// <summary>
    /// The other side of the boundary, where the divisor is not number-initial and both tools agree.
    /// Here so that narrowing or widening the divergence fails rather than passing quietly.
    /// </summary>
    public static IEnumerable<object[]> ScalarDivisionAgreementTestCases =>
        new List<(string expression, string bothTools, Rational observation)>
        {
            ("f / x / y", "(f / x) / y", new Rational(10)),
            ("f / x * y", "(f / x) * y", new Rational(10)),
            ("f / x/2", "(f / x) / 2", new Rational(10)),
            ("f / g(2)/2", "(f / g(2)) / 2", new Rational(10)),
            // a multiplication first: the two groupings agree by associativity, so there is nothing to choose
            ("f * 1/2", "(f * 1) / 2", new Rational(10)),
            ("f * x / y", "(f * x) / y", new Rational(10)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ScalarDivisionAgreementTestCases))]
    public void ScalarDivisionAgreesWithRTaWWhereTheDivisorIsNotANumber(
        string expression,
        string bothTools,
        Rational observation)
    {
        var state = StateWithPrecedenceVariables();

        Assert.Equal(
            ParseCurveValueAt(bothTools, state, observation),
            ParseCurveValueAt(expression, state, observation));
    }

    public static IEnumerable<object[]> ReferenceProductCompositionPrecedenceTestCases =>
        new List<(string expression, string equivalent, string different, Rational observation)>
        {
            ("f comp g / x", "(f comp g) / x", "f comp (g / x)", new Rational(1)),
            ("f * x comp g", "(f * x) comp g", "f * (x comp g)", new Rational(1)),
            ("f * g comp h", "(f * g) comp h", "f * (g comp h)", new Rational(1)),
            ("f / x comp g", "(f / x) comp g", "f / (x comp g)", new Rational(1)),
            ("f(x) * g + y", "(f(x) * g) + y", "f(x) * (g + y)", new Rational(10)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ReferenceProductCompositionPrecedenceTestCases))]
    public void ReferenceProductCompositionPrecedenceMatchesExpectedParenthesizedForm(
        string expression,
        string equivalent,
        string different,
        Rational observation)
    {
        var state = StateWithReferencePrecedenceVariables();
        var actual = ParseCurveValueAt(expression, state, observation);
        var expected = ParseCurveValueAt(equivalent, state, observation);
        var unexpected = ParseCurveValueAt(different, state, observation);

        Assert.Equal(expected, actual);
        Assert.NotEqual(unexpected, actual);
    }

    public static IEnumerable<object[]> ReferenceCompositionAssociativityTestCases =>
        new List<(string expression, string leftAssociative, string rightAssociative, Rational observation)>
        {
            ("f comp g comp h", "(f comp g) comp h", "f comp (g comp h)", new Rational(1)),
            ("x comp f comp g", "(x comp f) comp g", "x comp (f comp g)", new Rational(1)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ReferenceCompositionAssociativityTestCases))]
    public void ReferenceCompositionAssociativityExamplesMatchParenthesizedForms(
        string expression,
        string leftAssociative,
        string rightAssociative,
        Rational observation)
    {
        var state = StateWithReferencePrecedenceVariables();
        var actual = ParseCurveValueAt(expression, state, observation);
        var left = ParseCurveValueAt(leftAssociative, state, observation);
        var right = ParseCurveValueAt(rightAssociative, state, observation);

        Assert.Equal(left, actual);
        Assert.Equal(right, actual);
    }

    private static Rational ParseCurveValueAt(string mppg, State state, Rational observation)
    {
        var curve = Assert.IsAssignableFrom<CurveExpression>(
            ExpressionParsing.Parse(mppg, state));

        return curve.ValueAt(observation).Compute();
    }

    public static IEnumerable<object[]> TwoCurvesThreeNumberVarSampleTestCases =>
        new List<(string mppg, Rational observation, Rational expected)>
        {
            ("(f + 3) + (x + y)", new Rational(10), new Rational(60)),
            ("(x + y) + (f + 3)", new Rational(10), new Rational(60)),
            ("(f + 3) + 5", new Rational(10), new Rational(58)),
            ("x + f * y", new Rational(10), new Rational(203)),
            ("x * f / y", new Rational(10), new Rational(75, 2)),
            ("f + x /\\ y", new Rational(10), new Rational(4)),
            ("x + f /\\ y", new Rational(10), new Rational(4)),
            ("f - x \\/ y", new Rational(10), new Rational(47)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(TwoCurvesThreeNumberVarSampleTestCases))]
    public void TwoCurvesThreeNumberVarExpressionsEvaluateToExpectedSample(
        string mppg,
        Rational observation,
        Rational expected)
    {
        var state = StateWithTwoCurvesAndThreeNumberVars();
        var expr = ExpressionParsing.Parse(mppg, state);
        var curve = Assert.IsAssignableFrom<CurveExpression>(expr);
        var actual = curve.ValueAt(observation).Compute();

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> TwoCurvesThreeNumberVarCurveTestCases =>
        new List<(string mppg, Curve expected)>
        {
            (
                "(f + g) + (x + y)",
                new RateLatencyServiceCurve(10, 5)
                    .Addition(ServiceCurve())
                    .VerticalShift(new Rational(7))
            ),
            (
                "f * g / x",
                new RateLatencyServiceCurve(10, 5)
                    .Convolution(ServiceCurve())
                    .Scale(new Rational(1, 3))
            ),
            (
                "f * x + g",
                new RateLatencyServiceCurve(10, 5)
                    .Scale(new Rational(3))
                    .Addition(ServiceCurve())
            ),
            (
                "f + x * g",
                new RateLatencyServiceCurve(10, 5)
                    .Addition(ServiceCurve().Scale(new Rational(3)))
            ),
            (
                "f comp g + x",
                new RateLatencyServiceCurve(10, 5)
                    .Composition(ServiceCurve())
                    .Addition(ConstantFunction(new Rational(3)))
            ),
            (
                "f + x comp g",
                new RateLatencyServiceCurve(10, 5)
                    .Addition(ConstantFunction(new Rational(3)))
            ),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(TwoCurvesThreeNumberVarCurveTestCases))]
    public void TwoCurvesThreeNumberVarExpressionsEvaluateToExpectedCurve(
        string mppg,
        Curve expected)
    {
        var state = StateWithTwoCurvesAndThreeNumberVars();
        var expr = ExpressionParsing.Parse(mppg, state);
        var curve = Assert.IsAssignableFrom<CurveExpression>(expr);

        Assert.True(Curve.Equivalent(expected, curve.Compute()));
    }

    public static IEnumerable<object[]> FunctionSampleArithmeticTestCases =>
        new List<(string mppg, Rational expected)>
        {
            ("f(10 + 5)", new Rational(33)),
            ("f(10 - 5)", new Rational(13)),
            ("f(10 * 5)", new Rational(103)),
            ("f(3 + 5 ~-)", new Rational(19)),
            ("f(3 + 5 ~+)", new Rational(19)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FunctionSampleArithmeticTestCases))]
    public void FunctionSampleArithmeticEvaluatesToExpectedResult(
        string mppg,
        Rational expected)
    {
        var state = StateWithVariables();
        var expr = ExpressionParsing.Parse(mppg, state);
        var result = Assert.IsAssignableFrom<RationalExpression>(expr);

        Assert.Equal(expected, result.Compute());
    }

    public static IEnumerable<object[]> StateWithVariablesCurveSampleTestCases =>
        new List<(string mppg, Rational observation, Rational expected)>
        {
            ("f + (10 + 5)", new Rational(10), new Rational(38)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(StateWithVariablesCurveSampleTestCases))]
    public void StateWithVariablesCurveExpressionsEvaluateToExpectedSample(
        string mppg,
        Rational observation,
        Rational expected)
    {
        var state = StateWithVariables();
        var expr = ExpressionParsing.Parse(mppg, state);
        var curve = Assert.IsAssignableFrom<CurveExpression>(expr);
        var actual = curve.ValueAt(observation).Compute();

        Assert.Equal(expected, actual);
    }
}
