using Unipi.Nancy.Expressions;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.NetworkCalculus;
using Xunit.Abstractions;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class CurveParsing
{
    private readonly ITestOutputHelper _testOutputHelper;

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
}