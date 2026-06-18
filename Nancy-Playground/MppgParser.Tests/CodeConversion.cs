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
            ("hDev(f + (0), f)", true, "Expressions.HorizontalDeviation"),
            ("vDev(+f, f + (0))", true, "Expressions.VerticalDeviation"),
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
            Assert.Contains("!Curve.Equivalent(f.Compute(), g.Compute())", assertionLine);
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

    public static IEnumerable<object[]> FunctionOperatorConversionTestCases =>
        new List<(string expression, string expectedNancy, string expectedExpressions)>
        {
            ("f \\/ 3", "Curve.Maximum(f, new Curve", ".Maximum(Expressions.FromCurve"),
            ("0 /\\ f", "Curve.Minimum(f, new Curve", ".Minimum(Expressions.FromCurve"),
            ("vshift(f, 3)", ".VerticalShift(", ".VerticalShift("),
            ("up_inv(f)", ".UpperPseudoInverse()", ".UpperPseudoInverse()"),
            ("upclosure(f)", ".ToUpperNonDecreasing()", ".ToUpperNonDecreasing()"),
            ("left-ext(f)", ".ToLeftContinuous()", ".ToLeftContinuous()"),
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
}
