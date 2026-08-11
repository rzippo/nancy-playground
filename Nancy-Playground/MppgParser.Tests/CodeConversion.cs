using System.Text.RegularExpressions;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class CodeConversion
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionPreservesSignedNumberLiterals(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            f := ratency(1, 2)
            v := f + -3
            n := - 4
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("new Rational(-3)", fullCode);
        if (useNancyExpressions)
            Assert.Contains("(Expressions.FromRational(new Rational(4))).Negate()", fullCode);
        else
            Assert.Contains("-(new Rational(4))", fullCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionEmitsNamedRationalInfinityConstants(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            inf1 := +inf
            inf2 := -inf
            f := uaf( [(0,0)1(+inf,+inf)[ )
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("Rational.PlusInfinity", fullCode);
        Assert.Contains("Rational.MinusInfinity", fullCode);
        Assert.DoesNotContain("new Rational(1, 0)", fullCode);
        Assert.DoesNotContain("new Rational(-1, 0)", fullCode);
    }

    public static IEnumerable<object[]> FunctionValueAtTestCases =>
        new List<(string expression, bool useNancyExpressions)>
        {
            ("f(0)", false),
            ("f(0)", true),
            ("f((10))", false),
            ("f((10))", true),
            ("f(+(5))", false),
            ("f(+(5))", true),
            ("+f(10)", false),
            ("+f(10)", true),
            ("f(f(10))", false),
            ("f(f(10))", true),
            ("f(10 + x)", false),
            ("f(10 + x)", true),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FunctionValueAtTestCases))]
    public void FunctionValueAtConversionEmitsValueAtCall(
        string expression,
        bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            $"""
            f := ratency(10, 5)
            x := 3
            {expression}
            """,
            useNancyExpressions);

        var expressionLine = code.Single(line => line.StartsWith("Console.WriteLine("));

        Assert.Contains(".ValueAt(", expressionLine);
        Assert.DoesNotContain("NOT IMPLEMENTED", string.Join(Environment.NewLine, code));
    }

    public static IEnumerable<object[]> DeviationConversionTestCases =>
        new List<(string expression, bool useNancyExpressions, string expectedCall)>
        {
            ("hDev(f + (0), f)", false, "Curve.HorizontalDeviation"),
            ("vDev(+f, f + (0))", false, "Curve.VerticalDeviation"),
            ("zDev(f + (0), f)", false, "Curve.ZDeviation"),
            ("hDev(f + (0), f)", true, "Expressions.HorizontalDeviation"),
            ("vDev(+f, f + (0))", true, "Expressions.VerticalDeviation"),
            ("zDev(f + (0), f)", true, "Expressions.ZDeviation"),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(DeviationConversionTestCases))]
    public void DeviationConversionAcceptsParenthesizedScalarOperands(
        string expression,
        bool useNancyExpressions,
        string expectedCall)
    {
        var code = Program.ToNancyCode(
            $"""
            f := ratency(10, 5)
            {expression}
            """,
            useNancyExpressions);

        var expressionLine = code.Single(line => line.StartsWith("Console.WriteLine("));

        Assert.Contains(expectedCall, expressionLine);
        Assert.DoesNotContain("NOT IMPLEMENTED", string.Join(Environment.NewLine, code));
    }

    public static IEnumerable<object[]> CurveConstantAssertionTestCases =>
        new List<(string assertion, bool useNancyExpressions)>
        {
            ("assert(f >= 1)", false),
            ("assert(1 <= f)", false),
            ("assert(f >= 1)", true),
            ("assert(1 <= f)", true),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(CurveConstantAssertionTestCases))]
    public void CurveConstantAssertionConversionWrapsScalarAsConstantCurve(
        string assertion,
        bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            $"""
            f := bucket(2, 3)
            {assertion}
            """,
            useNancyExpressions);

        var assertionLine = code.Single(line => line.StartsWith("Console.WriteLine("));

        Assert.Contains("new Curve", assertionLine);
        Assert.DoesNotContain("f >= new Rational(1)", assertionLine);
        Assert.DoesNotContain("new Rational(1) <= f", assertionLine);
        Assert.DoesNotContain("f.Compute() >= Expressions.FromRational", assertionLine);
        Assert.DoesNotContain("Expressions.FromRational(new Rational(1)).Compute() <= f.Compute()", assertionLine);
    }

    public static IEnumerable<object[]> CompoundAssertionSideTestCases =>
        new List<string>
        {
            "assert( x = 7/2 )",
            "assert( x / 2 = 7/4 )",
            "assert( x - 1 != 3 )",
            "assert( f(3) + 1 = 3 )",
            "assert( 3 = f(3) + 1 )",
            "assert( f + 1 >= 1 )",
        }.ToXUnitTestCases();

    // Appended to a compound side, .Compute() would apply to its last operand only: the emitted code
    // would then compare expressions instead of values, when it compiles at all.
    [Theory]
    [MemberData(nameof(CompoundAssertionSideTestCases))]
    public void AssertionConversionComputesEachSideAsAWhole(string assertion)
    {
        var code = Program.ToNancyCode(
            $"""
            x := 7/2
            f := ratency(1, 1)
            {assertion}
            """,
            useNancyExpressions: true);

        var assertionLine = code.Single(line => line.StartsWith("Console.WriteLine("));

        Assert.DoesNotContain("NOT IMPLEMENTED", assertionLine);
        foreach (var computed in Regex.Matches(assertionLine, @".{1}\.Compute\(\)").Select(m => m.Value))
            Assert.Equal(").Compute()", computed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionPreservesInlineComments(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            // heading // inline
            x := 1 // first value
            x := x + 1 // second value
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("// heading // inline", fullCode);
        Assert.Contains("// first value", fullCode);
        Assert.Contains("// second value", fullCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionHandlesReassignmentsWithoutRedeclaring(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            x := 1
            x := x + 1
            f := affine(1, 0)
            f := f + 1
            """,
            useNancyExpressions);

        var xDeclarations = code.Count(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"^\w+\s+x\s*="));
        var xAssignments = code.Count(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"^x\s*="));
        var fDeclarations = code.Count(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"^\w+\s+f\s*="));
        var fAssignments = code.Count(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"^f\s*="));

        Assert.Equal(1, xDeclarations);
        Assert.Equal(1, xAssignments);
        Assert.Equal(1, fDeclarations);
        Assert.Equal(1, fAssignments);
    }

    public static IEnumerable<object[]> AssertionOperatorConversionTestCases =>
        new List<(string assertion, string expected, bool useNancyExpressions)>
        {
            ("assert(1 != 2)", " != ", false),
            ("assert(1 < 2)", " < ", false),
            ("assert(2 > 1)", " > ", false),
            ("assert(1 != 2)", " != ", true),
            ("assert(1 < 2)", " < ", true),
            ("assert(2 > 1)", " > ", true),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(AssertionOperatorConversionTestCases))]
    public void AssertionConversionEmitsExpectedComparisonOperator(
        string assertion,
        string expected,
        bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(assertion, useNancyExpressions);

        var assertionLine = code.Single(line => line.StartsWith("Console.WriteLine("));

        Assert.Contains(expected, assertionLine);
        Assert.DoesNotContain("NOT IMPLEMENTED", assertionLine);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FunctionInequalityConversionUsesCurveEquivalence(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            f := affine(1, 0)
            g := affine(2, 0)
            assert(f != g)
            """,
            useNancyExpressions);

        var assertionLine = code.Single(line => line.StartsWith("Console.WriteLine("));

        if (useNancyExpressions)
            Assert.Contains("!Curve.Equivalent((f).Compute(), (g).Compute())", assertionLine);
        else
            Assert.Contains("!Curve.Equivalent(f, g)", assertionLine);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlotConversionEmitsLabelsExtensionlessOutputAndTemporaryOutput(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            f := affine(1, 0)
            plot(f, main = "curve", xlab = "time", ylab = "value", out = "coverage", gui = "no")
            plot(f, gui = "no")
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("Title = $\"curve\"", fullCode);
        Assert.Contains("XLabel = $\"time\"", fullCode);
        Assert.Contains("YLabel = $\"value\"", fullCode);
        Assert.Contains("coverage.png", fullCode);
        Assert.Contains("plotTmpPath", fullCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlotTikzConversionEmitsTikzCodeAndDependency(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            f := affine(1, 0)
            g := ratency(1, 3)
            plotTikz(f, g, xlim = [0, 10], out = "coverage")
            plotTikz(f)
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("#:package Unipi.Nancy.Plots.Tikz@1.0.9", fullCode);
        Assert.Contains("using Unipi.Nancy.Plots.Tikz;", fullCode);
        Assert.Contains("TikzPlots.ToTikzPlotCode(", fullCode);
        // the names of the functions to plot are passed explicitly, to be used in the legend
        Assert.Contains("[\"f\", \"g\"]", fullCode);
        Assert.Contains("XLimit = new Interval(0, 10)", fullCode);
        // an extensionless out path gets the .tex extension, and the code is written there
        Assert.Contains("File.WriteAllText(\"coverage.tex\", plotTikzCode);", fullCode);
        // without out, the TikZ code is printed instead
        Assert.Contains("Console.WriteLine(plotTikzCode);", fullCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionOmitsTikzDependencyWithoutTikzPlots(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            f := affine(1, 0)
            plot(f, gui = "no")
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.DoesNotContain("Unipi.Nancy.Plots.Tikz", fullCode);
    }

    public static IEnumerable<object[]> FunctionOperatorConversionTestCases =>
        new List<(string expression, string expectedNancy, string expectedExpressions)>
        {
            ("f \\/ 3", "Curve.Maximum(f, new Curve", ".Maximum(Expressions.FromCurve"),
            ("0 /\\ f", "Curve.Minimum(f, new Curve", ".Minimum(Expressions.FromCurve"),
            ("vshift(f, 3)", ".VerticalShift(", ".VerticalShift("),
            ("up_inv(f)", ".UpperPseudoInverse()", ".UpperPseudoInverse()"),
            ("upclosure(f)", ".ToUpperNonDecreasing()", ".ToUpperNonDecreasing()"),
            ("lowclosure(f)", ".ToLowerNonDecreasing()", ".ToLowerNonDecreasing()"),
            ("nnlowclosure(f)", ".ToNonNegative().ToLowerNonDecreasing()", ".ToNonNegative().ToLowerNonDecreasing()"),
            ("left-ext(f)", ".ToLeftContinuous()", ".ToLeftContinuous()"),
            ("subaddclosure(f)", ".SubAdditiveClosure(", ".SubAdditiveClosure()"),
            ("superaddclosure(f)", ".SuperAdditiveClosure(", ".SuperAdditiveClosure()"),
            ("floor(f)", ".Floor()", ".Floor()"),
            ("ceil(f)", ".Ceil()", ".Ceil()"),
            // Rational.Floor() returns a BigInteger, so the emitted Nancy code casts back to Rational
            // to keep what surrounds it rational; on expressions the return type is already right
            ("f * floor(3/2)", "((Rational)(", ".Floor()"),
            ("f * ceil(3/2)", "((Rational)(", ".Ceil()"),
            ("f * abs(3)", "Rational.Abs(", ".AbsoluteValue()"),
            ("f * pow(2, 3)", "Rational.Pow(", ".Pow("),
            ("f * mod(7, 3)", "Rational.Remainder(", ".Remainder("),
            ("f * gcd(4, 6)", "Rational.GreatestCommonDivisor(", ".GreatestCommonDivisor("),
            ("f * lcm(4, 6)", "Rational.LeastCommonMultiple(", ".LeastCommonMultiple("),
        }
        .SelectMany(
            testCase => new[]
            {
                new object[] { testCase.expression, false, testCase.expectedNancy },
                new object[] { testCase.expression, true, testCase.expectedExpressions }
            });

    [Theory]
    [MemberData(nameof(FunctionOperatorConversionTestCases))]
    public void FunctionOperatorConversionEmitsExpectedCall(
        string expression,
        bool useNancyExpressions,
        string expected)
    {
        var code = Program.ToNancyCode(
            $"""
            f := affine(1, 0)
            result := {expression}
            result(1)
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains(expected, fullCode);
        Assert.DoesNotContain("NOT IMPLEMENTED", fullCode);
    }

    // Rational.Floor() and Rational.Ceil() return a BigInteger: emitted without a cast back to
    // Rational, a division between two of them would be an integer division, and print 0 instead of 3/4.
    [Fact]
    public void ScalarFloorConversionKeepsRationalDivision()
    {
        var code = Program.ToNancyCode(
            """
            floor(7/2) / floor(9/2)
            """,
            useNancyExpressions: false);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("((Rational)(new Rational(7) / new Rational(2)).Floor())", fullCode);
        Assert.DoesNotContain("NOT IMPLEMENTED", fullCode);
    }
}
