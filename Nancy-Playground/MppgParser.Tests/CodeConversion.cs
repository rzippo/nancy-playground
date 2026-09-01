using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unipi.Nancy.Playground.MppgParser.Utility;
using Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class CodeConversion
{
    /// <summary>
    /// A sign in front of a number is part of the literal, wherever the literal appears and whatever separates the two.
    /// The lexer skips the blanks, so '- 4' and '-4' are the same tokens, and both are one negative literal rather than a negation applied to a positive one.
    /// </summary>
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
            m := -5
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains("new Rational(-3)", fullCode);
        Assert.Contains("new Rational(-4)", fullCode);
        Assert.Contains("new Rational(-5)", fullCode);
    }

    /// <summary>
    /// A sign in front of anything a literal cannot spell stays a negation, which is what the sign alternatives of the unary tier are for.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConversionEmitsNegationForWhatIsNotALiteral(bool useNancyExpressions)
    {
        var code = Program.ToNancyCode(
            """
            x := 4
            n := -x
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        if (useNancyExpressions)
            Assert.Contains("(x).Negate()", fullCode);
        else
            Assert.Contains("-(x)", fullCode);
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

        Assert.Contains($"#:package Unipi.Nancy.Plots.Tikz@{PackageVersions.Tikz}", fullCode);
        Assert.Contains("using Unipi.Nancy.Plots.Tikz;", fullCode);
        Assert.Contains("TikzPlots.ToTikzPlotCode(", fullCode);
        // the names of the functions to plot are passed explicitly, to be used in the legend
        Assert.Contains("[\"f\", \"g\"]", fullCode);
        Assert.Contains("XLimit = new Interval(0, 10)", fullCode);
        // an extensionless out path gets the .tikz extension, and the code is written there
        Assert.Contains("File.WriteAllText(\"coverage.tikz\", plotTikzCode);", fullCode);
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
            // upnondec/upnondecclosure etc. are the 1.4 aliases of the closures above: same production, same emitted call.
            ("upnondec(f)", ".ToUpperNonDecreasing()", ".ToUpperNonDecreasing()"),
            ("upnondecclosure(f)", ".ToUpperNonDecreasing()", ".ToUpperNonDecreasing()"),
            ("nnupnondecclosure(f)", ".ToNonNegative().ToUpperNonDecreasing()", ".ToNonNegative().ToUpperNonDecreasing()"),
            ("lownondecclosure(f)", ".ToLowerNonDecreasing()", ".ToLowerNonDecreasing()"),
            ("nnlownondec(f)", ".ToNonNegative().ToLowerNonDecreasing()", ".ToNonNegative().ToLowerNonDecreasing()"),
            ("upnoninc(f)", ".ToUpperNonIncreasing()", ".ToUpperNonIncreasing()"),
            ("upnonincclosure(f)", ".ToUpperNonIncreasing()", ".ToUpperNonIncreasing()"),
            ("lownoninc(f)", ".ToLowerNonIncreasing()", ".ToLowerNonIncreasing()"),
            ("lownonincclosure(f)", ".ToLowerNonIncreasing()", ".ToLowerNonIncreasing()"),
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
            ("f * (7 mod 3)", "Rational.Remainder(", ".Remainder("),
            ("f * gcd(4, 6)", "Rational.GreatestCommonDivisor(", ".GreatestCommonDivisor("),
            ("f * lcm(4, 6)", "Rational.LeastCommonMultiple(", ".LeastCommonMultiple("),
            ("f + 1/2", ".VerticalShift(new Rational(1) / new Rational(2))", ".VerticalShift(Expressions.FromRational(new Rational(1)) / Expressions.FromRational(new Rational(2)))"),
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

    // The expected text is the Nancy member name alone: the direct-API profile reads it straight off
    // the operand ((f).IsX), the Expressions one materializes first (((f).Compute()).IsX), and both
    // wrap the whole access in a further "(!...)" when negated, so the member name is what is stable
    // to match across all four combinations this theory covers.
    public static IEnumerable<object[]> PropertyAssertionConversionTestCases =>
        new List<(string statement, string expected)>
        {
            ("assert(f is subadditive)", "IsSubAdditive"),
            ("assert(f is not superadditive)", "IsSuperAdditive"),
            ("assert(f is ua)", "IsUltimatelyAffine"),
            ("assert(f is ultimatelyaffine)", "IsUltimatelyAffine"),
        }
        .SelectMany(
            testCase => new[]
            {
                new object[] { testCase.statement, false, testCase.expected },
                new object[] { testCase.statement, true, testCase.expected }
            });

    [Theory]
    [MemberData(nameof(PropertyAssertionConversionTestCases))]
    public void PropertyAssertionConversionEmitsExpectedCall(
        string statement,
        bool useNancyExpressions,
        string expected)
    {
        var code = Program.ToNancyCode(
            $"""
            f := affine(1, 0)
            {statement}
            """,
            useNancyExpressions);

        var fullCode = string.Join(Environment.NewLine, code);

        Assert.Contains(expected, fullCode);
        Assert.DoesNotContain("NOT IMPLEMENTED", fullCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionEmitsPropertyAssertions(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := affine(1, 0)
            x := 7/2
            assert(f is subadditive)
            assert(f is not superadditive)
            assert(x is integer)
            assert(x is finite)
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        // The Expressions profile materializes first (f.Compute().IsX), the direct-API one reads the
        // property straight off the value (f.IsX); both are covered by matching the member name alone.
        Assert.DoesNotContain("NOT IMPLEMENTED", code);
        Assert.Contains("IsSubAdditive", code);
        Assert.Contains("!" + (useNancyExpressions ? "f.Compute()" : "f") + ".IsSuperAdditive", code);
        Assert.Contains("IsInteger", code);
        Assert.Contains("IsFinite", code);
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

    [Theory]
    [InlineData(false, "Rational")]
    [InlineData(true, "RationalExpression")]
    public void CodeTreeConversionEmitsScalarDeclarationsAndAssignments(
        bool useNancyExpressions,
        string expectedType)
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 1
            x := x + 1
            """,
            useNancyExpressions);

        var statements = tree.Members
            .OfType<GlobalStatementSyntax>()
            .Select(member => member.Statement)
            .ToList();
        var declaration = Assert.IsType<LocalDeclarationStatementSyntax>(statements[1]);
        var assignment = Assert.IsType<ExpressionStatementSyntax>(statements[2]);
        var assignmentExpression = Assert.IsType<AssignmentExpressionSyntax>(assignment.Expression);

        Assert.Equal(expectedType, declaration.Declaration.Type.ToString());
        Assert.IsType<BinaryExpressionSyntax>(assignmentExpression.Right);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionEmitsScalarUnaryOperators(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            a := floor(7/2)
            b := ceil(7/2)
            c := abs(-3)
            d := pow(2, 5)
            e := gcd(12, 18)
            g := lcm(4, 6)
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("NOT IMPLEMENTED", code);
        Assert.Contains("Floor()", code);
        Assert.Contains("Ceil()", code);
        Assert.Contains("GreatestCommonDivisor(", code);
        Assert.Contains("LeastCommonMultiple(", code);
        Assert.Contains(useNancyExpressions ? "AbsoluteValue()" : "Rational.Abs(", code);
        Assert.Contains(useNancyExpressions ? ".Pow(" : "Rational.Pow(", code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionEmitsNonIncreasingClosuresAndAliases(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := affine(1, 0)
            a := upnoninc(f)
            b := lownonincclosure(f)
            c := upnondecclosure(f)
            d := nnlownondec(f)
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("NOT IMPLEMENTED", code);
        Assert.Contains(".ToUpperNonIncreasing()", code);
        Assert.Contains(".ToLowerNonIncreasing()", code);
        Assert.Contains(".ToUpperNonDecreasing()", code);
        Assert.Contains(".ToNonNegative().ToLowerNonDecreasing()", code);
    }

    /// <summary>
    /// A construct with no dedicated code-tree visitor override must not abort the whole conversion:
    /// only the statement it appears in is reported as NOT IMPLEMENTED, while statements before and
    /// after it, which are implemented, still convert to real code.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionIsolatesUnimplementedStatements(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := ratency(1, 2)
            #!unknown-directive
            x := 3 + 4
            x
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Single(Regex.Matches(code, "NOT IMPLEMENTED"));
        Assert.Contains("RateLatencyServiceCurve", code);
        Assert.Contains(
            useNancyExpressions
                ? "Expressions.FromRational(3) + Expressions.FromRational(4)"
                : "Rational x = 3 + 4;",
            code);
        Assert.Contains("Console.WriteLine", code);
    }

    [Fact]
    public void CodeTreeConversionRemovesRedundantParenthesesFromInvocationArguments()
    {
        var tree = Program.ToNancyCodeTree(
            """
            C := affine(1, 0)
            A1 := stair(0, 60, 35)
            D1 := C + (A1 - C) * zero
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Curve.Convolution(A1 - C, Curve.Zero())", code);
        Assert.DoesNotContain("Curve.Convolution((A1 - C), Curve.Zero())", code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionRemovesRedundantParenthesesFromPrimaryReceivers(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := bucket(5, 2) * delay(1)
            g := up_inv(f)
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("g = f.UpperPseudoInverse();", code);
        Assert.DoesNotContain("(f).UpperPseudoInverse()", code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionKeepsRequiredParenthesesForBinaryReceivers(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := affine(1, 0)
            g := nnupclosure(f - f)
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("(f - f).ToNonNegative().ToUpperNonDecreasing()", code);
    }

    /// <summary>
    /// Regression test: a cast built defensively double-wrapped (once by the cast's own builder,
    /// once by the negation wrapping its operand) must still fully unwrap once the two collapse
    /// into one during cleanup, rather than stopping at the first, now-stale, removability check.
    /// </summary>
    [Fact]
    public void CodeTreeConversionRemovesParenthesesAroundNegatedCast()
    {
        var tree = Program.ToNancyCodeTree(
            """
            m := - floor(7/2)
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Rational m = -(Rational)((Rational)7 / 2).Floor();", code);
    }

    /// <summary>
    /// Regression test: a second plot command must not redeclare the plotBytes/plotTikzCode/plotTmpPath
    /// local a first one already declared. C# top-level statements share one flat scope, so repeating
    /// "var name = ..." is a duplicate-declaration compile error; a later plot must assign instead.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionReusesPlotTemporaryAcrossRepeatedPlots(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := ratency(1, 2)
            g := bucket(2, 1)
            plot(f)
            plot(g)
            """,
            useNancyExpressions);

        var statements = tree.Members
            .OfType<GlobalStatementSyntax>()
            .Select(member => member.Statement)
            .ToList();
        var plotByteDeclarations = statements.OfType<LocalDeclarationStatementSyntax>()
            .Count(declaration => declaration.Declaration.Variables.Any(v => v.Identifier.Text == "plotBytes"));
        var plotByteAssignments = statements.OfType<ExpressionStatementSyntax>()
            .Count(statement => statement.Expression is AssignmentExpressionSyntax
            {
                Left: IdentifierNameSyntax { Identifier.Text: "plotBytes" }
            });

        Assert.Equal(1, plotByteDeclarations);
        Assert.Equal(1, plotByteAssignments);
    }

    [Fact]
    public void CodeTreeConversionFormatsPlotSettingsWithoutEmptyParentheses()
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := ratency(1, 2)
            plotTikz(f, xlim = [0, 10])
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("new TikzPlotSettings {", code);
        Assert.DoesNotContain("new TikzPlotSettings() {", code);
    }

    /// <summary>
    /// "settings" is an optional parameter on both plotting APIs; a plot with no options at all
    /// (only possible for plotTikz, since plot's image defaults are never empty) should omit the
    /// argument entirely rather than pass an empty initializer.
    /// </summary>
    [Fact]
    public void CodeTreeConversionOmitsEmptyPlotSettingsArgument()
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := ratency(1, 2)
            plotTikz(f)
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("settings:", code);
        Assert.DoesNotContain("TikzPlotSettings", code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeTreeConversionEmitsPrintExpressionCommand(bool useNancyExpressions)
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 1
            printExpression(x)
            """,
            useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("NOT IMPLEMENTED", code);
        Assert.Contains("Console.WriteLine(x);", code);
    }

    /// <summary>
    /// A syntax version directive is only meaningful in the preamble, at the top of the program, but
    /// the grammar also accepts it as an ordinary statement anywhere else; the convert path (unlike
    /// run) does not flag that as an error, so it must still convert instead of throwing.
    /// </summary>
    [Fact]
    public void CodeTreeConversionEmitsMidProgramVersionDirectiveAsComment()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 1
            #!syntax version 1.2
            x
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("NOT IMPLEMENTED", code);
        Assert.Contains("// #!syntax version 1.2", code);
        Assert.Contains("Console.WriteLine(x);", code);
    }

    [Fact]
    public void CodeTreeConversionOmitsPlotPackagesAndUsingsWhenScriptDoesNotPlot()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 1
            x
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("Plots.ScottPlot", code);
        Assert.DoesNotContain("Plots.Tikz", code);
        Assert.DoesNotContain("System.IO", code);
    }

    /// <summary>
    /// Regression test: a script that only calls plotTikz must not pull in the ScottPlot package,
    /// even though both plot commands share the same "does this program plot at all" check for
    /// System.IO. Only ScottPlots.ToScottPlotImage needs that package, and only plot calls it.
    /// </summary>
    [Fact]
    public void CodeTreeConversionOmitsScottPlotPackageForTikzOnlyScript()
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := ratency(1, 2)
            plotTikz(f)
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Plots.Tikz", code);
        Assert.DoesNotContain("Plots.ScottPlot", code);
        Assert.Contains("System.IO", code);
    }

    [Fact]
    public void CodeTreeConversionCombinesPlotLimitsAndOutPath()
    {
        var tree = Program.ToNancyCodeTree(
            """
            f := ratency(1, 2)
            plot(f, out = "out.png", xlim = [0, 10], ylim = [0, 20])
            plotTikz(f, out = "out.tex")
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.DoesNotContain("NOT IMPLEMENTED", code);
        Assert.Contains("XLimit = new Interval(0, 10)", code);
        Assert.Contains("YLimit = new Interval(0, 20)", code);
        Assert.Contains("File.WriteAllBytes(\"out.png\", plotBytes);", code);
        Assert.Contains("File.WriteAllText(\"out.tex\", plotTikzCode);", code);
    }

    /// <summary>
    /// The direct-API profile pins Unipi.Nancy.Analyzers explicitly, since Unipi.Nancy itself does not
    /// depend on it (unlike Unipi.Nancy.Expressions, which does, so --use-expressions gets the same
    /// analyzer transitively and does not need its own explicit pin).
    /// </summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CodeTreeConversionPinsAnalyzersOnlyWhereNotAlreadyTransitive(
        bool useNancyExpressions,
        bool expectsExplicitPin)
    {
        var tree = Program.ToNancyCodeTree("x := 1", useNancyExpressions);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        if (expectsExplicitPin)
            Assert.Contains("#:package Unipi.Nancy.Analyzers@", code);
        else
            Assert.DoesNotContain("Unipi.Nancy.Analyzers", code);
    }

    [Fact]
    public void CodeTreeConversionEmitsBareIntLiteralsInDirectApiMode()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 5
            f := ratency(1, 2)
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Rational x = 5;", code);
        Assert.Contains("new RateLatencyServiceCurve(1, 2)", code);
        Assert.DoesNotContain("new Rational", code);
    }

    /// <summary>
    /// Regression test for the reason new Rational(n) could not simply become a bare literal
    /// everywhere: two bare ints divided by C#'s native / resolve to int division and truncate,
    /// where Rational division does not. 7 / 2 must come out as 7/2, never as the truncated 3.
    /// </summary>
    [Fact]
    public void CodeTreeConversionCastsBareDivisionToAvoidIntegerTruncation()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 7 / 2
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Rational x = (Rational)7 / 2;", code);
    }

    /// <summary>
    /// The cast is needed only when neither side of a division is already Rational-typed: once one
    /// side is (a variable, here), the other's implicit conversion is enough and no cast is added.
    /// </summary>
    [Fact]
    public void CodeTreeConversionOmitsDivisionCastWhenOneSideIsAlreadyRational()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 5
            y := x / 2
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Rational y = x / 2;", code);
        Assert.DoesNotContain("(Rational)", code);
    }

    /// <summary>
    /// Addition, subtraction and multiplication of two bare ints give the same value as the
    /// equivalent Rational arithmetic, so unlike division they need no protecting cast.
    /// </summary>
    [Fact]
    public void CodeTreeConversionDoesNotCastSafeBareArithmetic()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 3 + 4 * 2 - 1
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Rational x = 3 + 4 * 2 - 1;", code);
    }

    [Fact]
    public void CodeTreeConversionKeepsFractionsExplicitInDirectApiMode()
    {
        // A decimal literal is the one numberLiteral token that is itself a genuine fraction
        // (Denominator != 1): unlike n/d, written with a slash, which the grammar parses as a
        // division of two integers, not as one literal.
        var tree = Program.ToNancyCodeTree(
            """
            x := 0.25
            """,
            useNancyExpressions: false);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Rational x = new Rational(1, 4);", code);
    }

    /// <summary>
    /// Unlike Unipi.Nancy.Numerics.Rational, RationalExpression has no implicit conversion from int,
    /// so the reduction stops at Expressions.FromRational's own argument: everything past it stays
    /// built from RationalExpression values, exactly as before.
    /// </summary>
    [Fact]
    public void CodeTreeConversionSimplifiesFromRationalArgumentInExpressionsMode()
    {
        var tree = Program.ToNancyCodeTree(
            """
            x := 5
            y := 0.25
            """,
            useNancyExpressions: true);

        var code = string.Join(Environment.NewLine, NancyCodeTreeRenderer.RenderLines(tree));

        Assert.Contains("Expressions.FromRational(5)", code);
        Assert.Contains("Expressions.FromRational(new Rational(1, 4))", code);
    }

    // ToNancyCode's syntaxVersion/force pair, what convert's --syntax-version/--syntax-version-forced
    // resolve to; Program.FromText's own pair is covered in SyntaxVersioning.cs.
    // pow (1.3) is the probe, rather than printExpression: the legacy string visitor does not implement
    // printExpression at all, in any version, which would make a "did it convert" assertion meaningless.

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToNancyCodeSyntaxVersionFillsInWhereTheTextDeclaresNone(bool useCodeTrees)
    {
        Assert.Throws<Exceptions.SyntaxErrorException>(() =>
            Program.ToNancyCode(
                "x := pow(2, 3)",
                useCodeTrees: useCodeTrees,
                syntaxVersion: new SyntaxVersion(1, 2)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToNancyCodeSyntaxVersionLosesToTheTextsOwnDirective(bool useCodeTrees)
    {
        var code = Program.ToNancyCode(
            "#!syntax version 1.3\nx := pow(2, 3)",
            useCodeTrees: useCodeTrees,
            syntaxVersion: new SyntaxVersion(1, 2));

        Assert.Contains(code, line => line.Contains(".Pow("));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToNancyCodeForcedSyntaxVersionWinsOverTheTextsOwnDirective(bool useCodeTrees)
    {
        Assert.Throws<Exceptions.SyntaxErrorException>(() =>
            Program.ToNancyCode(
                "#!syntax version 1.3\nx := pow(2, 3)",
                useCodeTrees: useCodeTrees,
                syntaxVersion: new SyntaxVersion(1, 2),
                force: true));
    }
}
