using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Playground.MppgParser.Statements;
using GrammarMppgLexer = Unipi.MppgParser.Grammar.MppgLexer;
using GrammarMppgParser = Unipi.MppgParser.Grammar.MppgParser;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class SemanticPredicateParsing
{
    public static IEnumerable<object[]> AmbiguousExpressionParseCases =>
        new List<string>
        {
            "x * y",
            "(x) * y",
            "f(x)",
            "hDev(f,g)",
            "vDev(f,g)",
            "zDev(f,g)",
            "5 * f",
            "x * f",
            "(x) * f",
            "f * x",
            "f * (x)",
            "5 + f",
            "1 + ratency(1,1)",
            "x + f",
            "x /\\ f",
            "f + x",
            "f /\\ x",
            "f * g",
            "(f) * g",
            "f(x) * g",
            "(f(x)) * g",
            "f(x) + g",
            "f(x) /\\ g",
            "f(x) \\/ g",
            "(x + y) * f",
            "(x * y) * f",
            "f * (x * y)",
            "(x + y) + f",
            "f + (x + y)",
            "x * f + y * g",
            "x + f * y",
            "x comp f",
            "f comp x",
            "f + x comp g",
            "f comp g * x",
            "x comp g * y",
            "f comp x * g",
            "f * x comp g",
            "x * f comp g",
            "f * g comp h",
            "f / x comp g",
            "f comp g / x",
            "f comp g *_ h",
            "f comp g *^ h",
            "f comp g /_ h",
            "f comp g /^ h",
            "f(x) * g + y",
            "f(x) + g * y",
            "f(x) - g",
            "g - f(x)",
            "g /\\ f(x)",
            "g \\/ f(x)",
            "f(x) comp g",
            "g comp f(x)",
            // floor and ceil take either kind of argument and return that kind
            "floor(x)",
            "ceil(x)",
            "floor(f)",
            "ceil(f)",
            "floor(f(x))",
            "floor(x) * f",
            "f * floor(x)",
            "floor(f) * g",
            "f * floor(g)",
            "floor(x) + f",
            "f + floor(x)",
            "floor(x) * y",
            "floor(x + y) * f",
            "floor(ceil(x))",
            "floor(ceil(f))",
            // the scalar operators, which are a scalar operand wherever they appear
            "abs(x)",
            "pow(x, y)",
            "x mod y",
            "gcd(x, y)",
            "lcm(x, y)",
            "abs(x) * f",
            "f * abs(x)",
            "f + gcd(x, y)",
            "gcd(x, y) + f",
            "f comp lcm(x, y)",
            "abs(f(x))",
            "gcd(f(x), y)",
            "abs(hDev(f, g))",
            "abs(x + y) * f",
            "floor(abs(x))",
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(AmbiguousExpressionParseCases))]
    public void AmbiguousExpressionParsesWithoutErrors(string mppg)
    {
        var (_, remainingTokenType, errors) = ParseExpression(mppg);

        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(TokenConstants.EOF, remainingTokenType);
    }

    [Fact]
    public void ProgramDeclarationsSeedFollowingStatements()
    {
        const string mppg = """
            x := 2
            f := bucket(2,3)
            g := x * f
            h := f * x
            n := x * x
            q := f(x)
            """;

        var errors = new List<SyntaxErrorInfo>();
        var inputStream = CharStreams.fromString(mppg);
        var lexer = new GrammarMppgLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new DiagnosticLexerErrorListener(errors, inputStream));

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GrammarMppgParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new DiagnosticParserErrorListener(errors));

        parser.program();

        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(GrammarMppgParser.VariableType.Number, parser.VariableTypes["x"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["f"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["g"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["h"]);
        Assert.Equal(GrammarMppgParser.VariableType.Number, parser.VariableTypes["n"]);
        Assert.Equal(GrammarMppgParser.VariableType.Number, parser.VariableTypes["q"]);
    }

    [Fact]
    public void AssertWithFunctionOperandParsesWithoutErrors()
    {
        const string mppg = """
            f := bucket(2, 3)
            assert(1 <= f)
            """;

        var program = Program.FromText(mppg);
        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
    }

    public static IEnumerable<object[]> FunctionExpressionParseCases =>
        new List<string>
        {
            "1 + ratency(1,1)",
            "x + f",
            "f(x) * g",
            "f(x) + g",
            "f(x) /\\ g",
            "5 * f",
            "f * g",
            "(x + y) * f",
            "f * (x * y)",
            "x comp f",
            "f comp x",
            "f + x comp g",
            "f comp g * x",
            "x comp g * y",
            "f comp x * g",
            "f * x comp g",
            "x * f comp g",
            "f * g comp h",
            "f / x comp g",
            "f comp g / x",
            "f comp g *_ h",
            "f comp g *^ h",
            "f comp g /_ h",
            "f comp g /^ h",
            "f(x) * g + y",
            "f(x) + g * y",
            "f(x) - g",
            "g - f(x)",
            "g /\\ f(x)",
            "g \\/ f(x)",
            "f(x) comp g",
            "g comp f(x)",
            // a floor or ceil of a function is a function, whatever surrounds it
            "floor(f)",
            "ceil(f)",
            "floor(f) * g",
            "f * floor(g)",
            "floor(f) + x",
            "floor(x) * f",
            "floor(ceil(f))",
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(FunctionExpressionParseCases))]
    public void ExpressionParsesAsFunctionExpression(string mppg)
    {
        var (context, _, errors) = ParseExpression(mppg);
        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));
        Assert.NotNull(context.functionExpression());
    }

    public static IEnumerable<object[]> MixedFunctionRouteParseCases =>
        new List<(string mppg, Type expectedType)>
        {
            ("f * g", typeof(GrammarMppgParser.FunctionMinPlusConvolutionSuffixContext)),
            ("f * x", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("x * f", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("(x) * f", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("f * (x)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * (x * y)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * (x / y)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * x + g", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f + x * g", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("(x * y) * f", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("(x / y) * f", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("f(x) * g", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("(f(x)) * g", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("g * f(x)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f / g", typeof(GrammarMppgParser.FunctionMinPlusDeconvolutionSuffixContext)),
            ("f / x", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("f / (x * y)", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("f / x * g", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("f + g", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("f + x", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("x + f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("(x) + f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f + (x)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("f + (x + y)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("(x + y) + f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f(x) + g", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("g + f(x)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("f + x comp g", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("f - g", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("f - x", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("x - f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f(x) - g", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f /\\ g", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("f /\\ x", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("x /\\ f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f /\\ (x /\\ y)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("(x /\\ y) /\\ f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f(x) /\\ g", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f \\/ g", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("f \\/ x", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("x \\/ f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f \\/ (x \\/ y)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("(x \\/ y) \\/ f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f(x) \\/ g", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f comp g", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("x comp f", typeof(GrammarMppgParser.FunctionScalarCompositionRevContext)),
            ("f comp x", typeof(GrammarMppgParser.FunctionScalarCompositionSuffixContext)),
            ("f + x comp g", typeof(GrammarMppgParser.FunctionScalarCompositionRevContext)),
            ("x + f comp y", typeof(GrammarMppgParser.FunctionScalarCompositionSuffixContext)),
            ("f comp g * x", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f comp g * x", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("x comp g * y", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("x comp g * y", typeof(GrammarMppgParser.FunctionScalarCompositionRevContext)),
            ("f comp x * g", typeof(GrammarMppgParser.FunctionMinPlusConvolutionSuffixContext)),
            ("f comp x * g", typeof(GrammarMppgParser.FunctionScalarCompositionSuffixContext)),
            ("f * x comp g", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * x comp g", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("x * f comp g", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("x * f comp g", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f * g comp h", typeof(GrammarMppgParser.FunctionMinPlusConvolutionSuffixContext)),
            ("f * g comp h", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f / x comp g", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("f / x comp g", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f comp g / x", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f comp g / x", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("f comp g *_ h", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f comp g *_ h", typeof(GrammarMppgParser.FunctionMinPlusConvolutionSuffixContext)),
            ("f comp g *^ h", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f comp g *^ h", typeof(GrammarMppgParser.FunctionMaxPlusConvolutionSuffixContext)),
            ("f comp g /_ h", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f comp g /_ h", typeof(GrammarMppgParser.FunctionMinPlusDeconvolutionSuffixContext)),
            ("f comp g /^ h", typeof(GrammarMppgParser.FunctionCompositionContext)),
            ("f comp g /^ h", typeof(GrammarMppgParser.FunctionMaxPlusDeconvolutionSuffixContext)),
            ("f(x) * g + y", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("f(x) * g + y", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("f(x) + g * y", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f(x) + g * y", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f(x) - g", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("g - f(x)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("g /\\ f(x)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("g \\/ f(x)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("f(x) comp g", typeof(GrammarMppgParser.FunctionScalarCompositionRevContext)),
            ("g comp f(x)", typeof(GrammarMppgParser.FunctionScalarCompositionSuffixContext)),
            // the argument of floor/ceil, not the keyword, decides which side of an operator it sits on
            ("floor(f) * g", typeof(GrammarMppgParser.FunctionMinPlusConvolutionSuffixContext)),
            ("f * floor(g)", typeof(GrammarMppgParser.FunctionMinPlusConvolutionSuffixContext)),
            ("floor(x) * f", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("f * floor(x)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * ceil(x)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f / floor(x)", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("f / floor(g)", typeof(GrammarMppgParser.FunctionMinPlusDeconvolutionSuffixContext)),
            ("floor(x) + f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f + floor(x)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("f + floor(g)", typeof(GrammarMppgParser.FunctionSumSubMinMaxSuffixContext)),
            ("floor(x) comp f", typeof(GrammarMppgParser.FunctionScalarCompositionRevContext)),
            ("f comp floor(x)", typeof(GrammarMppgParser.FunctionScalarCompositionSuffixContext)),
            ("f comp floor(g)", typeof(GrammarMppgParser.FunctionCompositionContext)),
            // the argument is scanned as a whole, so a scalar-returning call inside keeps it scalar
            ("f * floor(g(x))", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * floor(x + y)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            // a scalar operator is the scalar side of a mixed operator, on either side of it
            ("abs(x) * f", typeof(GrammarMppgParser.FunctionScalarMulRevContext)),
            ("f * abs(x)", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f / gcd(x, y)", typeof(GrammarMppgParser.FunctionScalarDivSuffixContext)),
            ("pow(x, y) + f", typeof(GrammarMppgParser.FunctionShiftMinMaxRevContext)),
            ("f + (x mod y)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("f /\\ lcm(x, y)", typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)),
            ("lcm(x, y) comp f", typeof(GrammarMppgParser.FunctionScalarCompositionRevContext)),
            ("f comp abs(x)", typeof(GrammarMppgParser.FunctionScalarCompositionSuffixContext)),
            // the arguments may be scalar-returning calls on curves without changing that
            ("f * abs(g(x))", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
            ("f * abs(hDev(f, g))", typeof(GrammarMppgParser.FunctionScalarMulSuffixContext)),
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(MixedFunctionRouteParseCases))]
    public void MixedFunctionExpressionsUseDedicatedAlternatives(string mppg, Type expectedType)
    {
        var (context, _, errors) = ParseExpression(mppg);

        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));
        var functionContext = context.functionExpression();
        Assert.NotNull(functionContext);
        Assert.NotNull(FindDescendant(functionContext, expectedType));
    }

    [Fact]
    public void BracketedNumberOperandsRemainNumberExpressions()
    {
        var (context, _, errors) = ParseExpression("f + (x)");

        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));

        var expressionContext = context.functionExpression();
        Assert.NotNull(expressionContext);

        var functionContext = Assert.IsType<GrammarMppgParser.FunctionShiftMinMaxSuffixContext>(
            FindDescendant(
                expressionContext,
                typeof(GrammarMppgParser.FunctionShiftMinMaxSuffixContext)));

        Assert.NotNull(functionContext.numberEnclosedExpression());
    }

    public static IEnumerable<object[]> InvalidNumberLeftDivisionByFunctionCases =>
        new List<string>
        {
            "x / f",
            "x div f",
            "(x / y) / f",
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(InvalidNumberLeftDivisionByFunctionCases))]
    public void NumberLeftDivisionByFunctionDoesNotParseAsMixedFunction(string mppg)
    {
        var (_, remainingTokenType, errors) = ParseExpression(mppg);

        Assert.True(errors.Count > 0 || remainingTokenType != TokenConstants.EOF);
    }

    public static IEnumerable<object[]> UnknownVariableReferenceParseCases =>
        new List<string>
        {
            "u",
            "u + x",
            "x + u",
            "u * f",
            "f * u",
            "u comp f",
            "f comp u",
            "f(u)",
            "hDev(f, u)",
            "vDev(u, g)",
            "zDev(f, u)",
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(UnknownVariableReferenceParseCases))]
    public void UnknownVariableReferencesDoNotParseAsExpressions(string mppg)
    {
        var (_, remainingTokenType, errors) = ParseExpression(mppg);

        Assert.True(errors.Count > 0 || remainingTokenType != TokenConstants.EOF);
    }

    public static IEnumerable<object[]> UnknownVariableReferenceProgramCases =>
        new List<string>
        {
            """
            x := 1
            y := missing + x
            """,
            """
            f := bucket(2, 3)
            g := f + missing
            """,
            """
            f := bucket(2, 3)
            sample := f(missing)
            """,
            """
            forward := later
            later := 1
            """,
            """
            f := bucket(2, 3)
            plot(f, main = "missing: " + missing)
            """,
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(UnknownVariableReferenceProgramCases))]
    public void ProgramRejectsUnknownVariableReferences(string mppg)
    {
        var program = Program.FromText(mppg);

        Assert.NotEmpty(program.Errors.Select(error => error.ToString(verbose: true)));
    }

    [Fact]
    public void PlotStringVariablesAcceptDeclaredNames()
    {
        const string mppg = """
            x := 2
            f := bucket(2, 3)
            plot(f, main = "x = " + x, title = "f = " + f, gui = "no")
            """;

        var program = Program.FromText(mppg);

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
    }

    private static IParseTree? FindDescendant(IParseTree context, Type expectedType)
    {
        if (context.GetType() == expectedType)
            return context;

        for (var i = 0; i < context.ChildCount; i++)
        {
            var descendant = FindDescendant(context.GetChild(i), expectedType);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }

    public static IEnumerable<object[]> NumberExpressionParseCases =>
        new List<string>
        {
            "hDev(f,g)",
            "vDev(f,g)",
            "zDev(f,g)",
            "f(x)",
            "f(x + y)",
            "f(x) + y",
            "f(x) - y",
            "f(x) * y",
            "f(x) / y",
            "f(x) /\\ y",
            "f(x) \\/ y",
            "vDev(f,g)",
            // a floor or ceil of a scalar is a scalar, and a scalar-returning call inside one is scanned
            // as part of its argument, so it does not turn the call into a function expression
            "floor(x)",
            "ceil(x)",
            "floor(3/2)",
            "floor(x + y)",
            "floor(x) * y",
            "floor(f(x))",
            "ceil(f(x))",
            "floor(hDev(f, g))",
            "floor(ceil(x))",
            "-floor(x)",
            // the scalar operators take scalars and return one, whatever their arguments are built from
            "abs(x)",
            "pow(x, y)",
            "x mod y",
            "gcd(x, y)",
            "lcm(x, y)",
            "abs(f(x))",
            "gcd(f(x), y)",
            "abs(hDev(f, g))",
            "gcd(x, y) * lcm(x, y)",
            "abs(x + y)",
            "floor(abs(x))",
            "abs(floor(x))",
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(NumberExpressionParseCases))]
    public void NumberReturningCallParsesAsNumberExpression(string mppg)
    {
        var (context, _, errors) = ParseExpression(mppg);
        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));
        Assert.NotNull(context.numberExpression());
        Assert.Null(context.functionExpression());
    }

    public static IEnumerable<object[]> AssertionDelimiterParseCases =>
        new List<string>
        {
            "assert(1 <= f)",
            "assert(f >= 1)",
            "assert(f(x) <= 10)",
            "assert(10 >= f(x))",
            "assert(0 <= x + f)",
            "assert(x + f >= 0)",
            "assert(f comp g * x <= y + h)",
            "assert(y + h >= f comp g * x)",
            "assert(f(x) * g + y >= h)",
            "assert(f(x) + y <= hDev(f, g) + x)",
            "assert(f(x) * y <= hDev(f, g) + x)",
        }.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(AssertionDelimiterParseCases))]
    public void AssertionDelimitersDoNotLeakFunctionOperandsIntoScalarSides(string assertion)
    {
        var mppg = $$"""
            x := 2
            y := 3
            f := bucket(2, 3)
            g := ratency(10, 5)
            h := affine(3, 0)
            {{assertion}}
            """;

        var program = Program.FromText(mppg);

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
    }

    [Fact]
    public void ProgramDeclarationsClassifyDubiousMixedExpressionsByResultType()
    {
        const string mppg = """
            x := 2
            y := 3
            f := bucket(2, 3)
            g := ratency(10, 5)
            h := affine(3, 0)
            sample := f(x)
            samplePlusNumber := f(x) + y
            sampleTimesNumber := f(x) * y
            sampleTimesFunction := f(x) * g
            samplePlusFunction := f(x) + g
            sampleMinusFunction := f(x) - g
            functionMinusSample := g - f(x)
            functionMinSample := g /\ f(x)
            functionMaxSample := g \/ f(x)
            sampleCompFunction := f(x) comp g
            functionCompSample := g comp f(x)
            groupedNumberTimesFunction := (x * y) * f
            functionTimesGroupedNumber := f * (x * y)
            groupedNumberPlusFunction := (x + y) + f
            functionPlusGroupedNumber := f + (x + y)
            precedenceMix := x + f * y
            scalarCompFunction := x comp f
            functionCompScalar := f comp x
            compositionPrecedence := f + x comp g
            compositionProductPrecedence := f comp g * x
            compositionProductLeftPrecedence := f * x comp g
            compositionFunctionProductLeftPrecedence := f * g comp h
            compositionDivisionLeftPrecedence := f / x comp g
            sampleProductBeforeSum := f(x) * g + y
            """;

        var errors = new List<SyntaxErrorInfo>();
        var inputStream = CharStreams.fromString(mppg);
        var lexer = new GrammarMppgLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new DiagnosticLexerErrorListener(errors, inputStream));

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GrammarMppgParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new DiagnosticParserErrorListener(errors));

        parser.program();

        Assert.Empty(errors.Select(error => error.ToString(verbose: true)));
        Assert.Equal(GrammarMppgParser.VariableType.Number, parser.VariableTypes["sample"]);
        Assert.Equal(GrammarMppgParser.VariableType.Number, parser.VariableTypes["samplePlusNumber"]);
        Assert.Equal(GrammarMppgParser.VariableType.Number, parser.VariableTypes["sampleTimesNumber"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["sampleTimesFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["samplePlusFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["sampleMinusFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionMinusSample"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionMinSample"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionMaxSample"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["sampleCompFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionCompSample"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["groupedNumberTimesFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionTimesGroupedNumber"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["groupedNumberPlusFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionPlusGroupedNumber"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["precedenceMix"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["scalarCompFunction"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["functionCompScalar"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["compositionPrecedence"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["compositionProductPrecedence"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["compositionProductLeftPrecedence"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["compositionFunctionProductLeftPrecedence"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["compositionDivisionLeftPrecedence"]);
        Assert.Equal(GrammarMppgParser.VariableType.Function, parser.VariableTypes["sampleProductBeforeSum"]);
    }

    [Fact]
    public void InteractiveStyleSequentialEvaluation()
    {
        var state = new State();

        var line1 = Statement.FromLine("x := 3", state);
        line1.Execute(state);
        var (exists1, type1) = state.GetVariableType("x");
        Assert.True(exists1);
        Assert.Equal(ExpressionType.Number, type1);

        var line2 = Statement.FromLine("f := ratency(10,5)", state);
        line2.Execute(state);
        var (exists2, type2) = state.GetVariableType("f");
        Assert.True(exists2);
        Assert.Equal(ExpressionType.Function, type2);

        var line3 = Statement.FromLine("x + f", state);
        var result3 = line3.Execute(state);
        Assert.NotEmpty(result3);

        var line4 = Statement.FromLine("f - x", state);
        var result4 = line4.Execute(state);
        Assert.NotEmpty(result4);

        var line5 = Statement.FromLine("x - f", state);
        var result5 = line5.Execute(state);
        Assert.NotEmpty(result5);
    }

    [Fact]
    public void ScriptedProgramWithMixedScalarCurve()
    {
        const string programText = """
            x := 3
            f := ratency(10, 5)
            x + f
            f - x
            x - f
            """;

        var program = Program.FromText(programText);
        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));

        var output = program.ExecuteToStringOutput().ToList();
        Assert.NotEmpty(output);
    }

    [Fact]
    public void ConvertNumericObservationOfMixedExpressions()
    {
        const string programText = """
            x := 3
            f := ratency(10, 5)
            plusLeft := x + f
            minusLeft := x - f
            """;

        var program = Program.FromText(programText);
        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));

        var output = program.ExecuteToStringOutput().ToList();
        Assert.NotEmpty(output);

        var (existsP, typePlus) = program.ProgramContext.State.GetVariableType("plusLeft");
        Assert.True(existsP);
        Assert.Equal(ExpressionType.Function, typePlus);

        var (existsM, typeMinus) = program.ProgramContext.State.GetVariableType("minusLeft");
        Assert.True(existsM);
        Assert.Equal(ExpressionType.Function, typeMinus);

        var plusCurve = program.ProgramContext.State.GetFunctionVariable("plusLeft");
        var plusAt10 = plusCurve.ValueAt(new Rational(10)).Compute();
        Assert.Equal(new Rational(53), plusAt10);

        var minusCurve = program.ProgramContext.State.GetFunctionVariable("minusLeft");
        var minusAt10 = minusCurve.ValueAt(new Rational(10)).Compute();
        Assert.Equal(new Rational(-47), minusAt10);
    }

    private static (GrammarMppgParser.ExpressionContext context, int remainingTokenType, List<SyntaxErrorInfo> errors)
        ParseExpression(string mppg)
    {
        var errors = new List<SyntaxErrorInfo>();
        var inputStream = CharStreams.fromString(mppg);
        var lexer = new GrammarMppgLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new DiagnosticLexerErrorListener(errors, inputStream));

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GrammarMppgParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new DiagnosticParserErrorListener(errors));

        parser.SetVariableType("f", GrammarMppgParser.VariableType.Function);
        parser.SetVariableType("g", GrammarMppgParser.VariableType.Function);
        parser.SetVariableType("h", GrammarMppgParser.VariableType.Function);
        parser.SetVariableType("x", GrammarMppgParser.VariableType.Number);
        parser.SetVariableType("y", GrammarMppgParser.VariableType.Number);

        var context = parser.expression();
        return (context, tokenStream.LA(1), errors);
    }
}
