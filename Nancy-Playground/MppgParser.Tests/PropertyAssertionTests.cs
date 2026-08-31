using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// <c>assert(f is X)</c>/<c>assert(f is not X)</c>, run-mode evaluation, type checking, negation and synonyms.
/// Emitted-code coverage lives in <see cref="CodeConversion"/>.
/// </summary>
public class PropertyAssertionTests
{
    private const string Declarations = """
        f := bucket(2, 3)
        g := ratency(1, 3)
        x := 7/2
        """;

    [Theory]
    [InlineData("f is subadditive", "true")]
    [InlineData("f is superadditive", "false")]
    [InlineData("g is superadditive", "true")]
    [InlineData("f is nonnegative", "true")]
    [InlineData("f is not superadditive", "true")]
    [InlineData("f is not subadditive", "false")]
    [InlineData("x is integer", "false")]
    [InlineData("x is not integer", "true")]
    [InlineData("x is finite", "true")]
    [InlineData("f is finite", "true")]
    public void PropertyAssertionEvaluatesCorrectly(string assertion, string expected)
    {
        var program = Program.FromText($"{Declarations}\nassert({assertion})");

        Assert.Empty(program.Errors);
        Assert.Equal(expected, program.ExecuteToStringOutput().Last());
    }

    [Theory]
    [InlineData("ua", "ultimatelyaffine")]
    [InlineData("uc", "ultimatelyconstant")]
    [InlineData("ui", "ultimatelyinfinite")]
    public void SynonymEvaluatesTheSameAsTheCanonicalName(string synonym, string canonical)
    {
        var synonymResult = Program.FromText($"{Declarations}\nassert(f is {synonym})").ExecuteToStringOutput().Last();
        var canonicalResult = Program.FromText($"{Declarations}\nassert(f is {canonical})").ExecuteToStringOutput().Last();

        Assert.Equal(canonicalResult, synonymResult);
    }

    /// <summary>
    /// A property that does not apply to the operand's kind is a parse-time error, not a silent
    /// "false": <c>subadditive</c> is function-only, and <c>x</c> is a number.
    /// </summary>
    [Fact]
    public void PropertyNotApplicableToOperandTypeIsAnError()
    {
        var program = Program.FromText($"{Declarations}\nassert(x is subadditive)");

        var error = Assert.IsType<SyntaxErrorStatement>(program.Statements[^1]);
        Assert.Contains("'subadditive' does not apply to a number", error.InnerException?.Message);
    }

    /// <summary>
    /// The reverse mismatch: <c>integer</c> is scalar-only, and <c>f</c> is a function.
    /// </summary>
    [Fact]
    public void ScalarOnlyPropertyOnAFunctionIsAnError()
    {
        var program = Program.FromText($"{Declarations}\nassert(f is integer)");

        var error = Assert.IsType<SyntaxErrorStatement>(program.Statements[^1]);
        Assert.Contains("'integer' does not apply to a function", error.InnerException?.Message);
    }

    /// <summary>
    /// A property shared between functions and numbers, under the same keyword, applies to either.
    /// </summary>
    [Theory]
    [InlineData("f")]
    [InlineData("x")]
    public void PropertySharedBetweenFunctionAndScalarAppliesToBoth(string operand)
    {
        var program = Program.FromText($"{Declarations}\nassert({operand} is finite)");

        Assert.Empty(program.Errors);
    }

    /// <summary>
    /// <c>zero</c> is a property name here and the constant-zero curve constructor elsewhere; the
    /// parser must still accept it as a property, since it can never lex as an <c>IDENTIFIER</c>.
    /// </summary>
    [Fact]
    public void ZeroIsAcceptedAsAPropertyDespiteAlsoBeingAConstructorKeyword()
    {
        var program = Program.FromText($"{Declarations}\nassert(x is not zero)");

        Assert.Empty(program.Errors);
        Assert.Equal("true", program.ExecuteToStringOutput().Last());
    }

    /// <summary>
    /// A property name stays usable as a variable, matching the contextual-keyword treatment
    /// <c>plotArg</c>'s option names already get: it is never a reserved token.
    /// </summary>
    [Fact]
    public void PropertyNameStaysUsableAsAVariable()
    {
        var program = Program.FromText("subadditive := 3\nsubadditive + 1");

        Assert.Empty(program.Errors);
    }

    /// <summary>
    /// 'is' and 'not' are contextual keywords too, recognised by text only inside assertionTail's
    /// property alternative, so unlike an ordinary 1.4 keyword (e.g. upnoninc) they stay usable as
    /// variable names even at the latest version, with no <c>#!syntax version</c> needed to keep them.
    /// </summary>
    [Fact]
    public void IsAndNotStayUsableAsVariableNames()
    {
        var program = Program.FromText("is := 3\nnot := 4\nis + not");

        Assert.Empty(program.Errors);
        Assert.Equal("7", program.ExecuteToStringOutput().Last());
    }

    /// <summary>
    /// The property form still requires 1.4, even though 'is' and 'not' are never reserved: an older
    /// version has no assertionTail alternative for them, so 'is' there falls through to a plain,
    /// undeclared variable reference rather than silently being accepted.
    /// </summary>
    [Theory]
    [InlineData("1.0")]
    [InlineData("1.3")]
    public void PropertyAssertionRequiresVersion1_4(string version)
    {
        var program = Program.FromText($"""
            #!syntax version {version}
            {Declarations}
            assert(f is subadditive)
            """);

        Assert.NotEmpty(program.Errors);
    }

    private const string KeywordNamedVariables = """
        is := ratency(1, 2)
        not := ratency(2, 1)
        subadditive := bucket(2, 3)
        superadditive := star(bucket(1, 1))
        """;

    /// <summary>
    /// The same word is both the operand and the keyword in one statement: a variable declared with a
    /// name that is also 'is', 'not', or a property name is still told apart from the keyword or
    /// property spelled the same way, since only position, not text, decides which is which.
    /// </summary>
    [Theory]
    [InlineData("is is subadditive", "false")]
    [InlineData("not is superadditive", "true")]
    [InlineData("is is not superadditive", "false")]
    [InlineData("subadditive is subadditive", "true")]
    [InlineData("superadditive is superadditive", "false")]
    [InlineData("subadditive is not superadditive", "true")]
    public void SameWordWorksAsBothOperandAndKeywordInOneStatement(string assertion, string expected)
    {
        var program = Program.FromText($"{KeywordNamedVariables}\nassert({assertion})");

        Assert.Empty(program.Errors);
        Assert.Equal(expected, program.ExecuteToStringOutput().Last());
    }

    /// <summary>
    /// A variable named 'is' or 'not' is an ordinary function variable everywhere outside
    /// assertionTail's property alternative: sampled, added, and compared like any other.
    /// </summary>
    [Fact]
    public void KeywordNamedFunctionVariableSupportsCallArithmeticAndComparison()
    {
        var program = Program.FromText($"""
            {KeywordNamedVariables}
            y := is(3)
            is2 := is + not
            assert(y = is(3))
            assert(is = is)
            assert(not != is)
            """);

        Assert.Empty(program.Errors);
        var results = program.ExecuteToStringOutput().Where(line => line is "true" or "false").ToList();
        Assert.Equal(3, results.Count);
        Assert.All(results, line => Assert.Equal("true", line));
    }

    /// <summary>
    /// A word reused where the property name itself is expected has no valid parse: 'is' and 'not'
    /// are never registered properties, so the slot after them must reject rather than silently
    /// match the wrong token.
    /// </summary>
    [Theory]
    [InlineData("is is is")]
    [InlineData("not is not")]
    [InlineData("is is not not")]
    public void WordReusedWherePropertyNameIsExpectedIsRejected(string assertion)
    {
        var program = Program.FromText($"{KeywordNamedVariables}\nassert({assertion})");

        Assert.NotEmpty(program.Errors);
    }

    /// <summary>
    /// Whether the second token after the operand is a comparison operator or 'is' decides the
    /// branch: a variable named 'is' does not pull an ordinary comparison into the property one.
    /// </summary>
    [Fact]
    public void KeywordNamedVariableWorksAsAnOrdinaryComparisonOperand()
    {
        var program = Program.FromText($"""
            {Declarations}
            is := 5
            assert(f = is)
            """);

        Assert.Empty(program.Errors);
        Assert.Equal("false", program.ExecuteToStringOutput().Last());
    }
}
