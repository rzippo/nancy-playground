using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class SyntaxVersioning
{
    [Fact]
    public void NoShebang_AllowsPrintExpression()
    {
        const string programText = """
        a := 1
        printExpression(a)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is PrintExpressionCommand { VariableName: "a" });
    }

    [Fact]
    public void PreambleShebangV1_0_RejectsPrintExpression()
    {
        const string programText = """
        #!syntax version 1.0
        a := 1
        printExpression(a)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.DoesNotContain(program.Statements, s => s is PrintExpressionCommand);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
    }

    [Fact]
    public void PreambleShebangV1_1_AllowsPrintExpression()
    {
        const string programText = """
        #!syntax version 1.1
        a := 1
        printExpression(a)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 1), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is PrintExpressionCommand { VariableName: "a" });
    }

    [Fact]
    public void NoShebang_AllowsPlotTikz()
    {
        const string programText = """
        f := ratency(1, 3)
        plotTikz(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is PlotTikzCommand);
    }

    [Fact]
    public void PreambleShebangV1_0_RejectsPlotTikz()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 3)
        plotTikz(f)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.DoesNotContain(program.Statements, s => s is PlotTikzCommand);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
    }

    [Fact]
    public void PreambleShebangV1_0_AllowsPlot()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 3)
        plot(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Contains(program.Statements, s => s is PlotCommand and not PlotTikzCommand);
    }

    [Fact]
    public void PreambleShebangV1_1_AllowsPlotTikz()
    {
        const string programText = """
        #!syntax version 1.1
        f := ratency(1, 3)
        plotTikz(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 1), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is PlotTikzCommand);
    }

    [Fact]
    public void InteractiveMode_VersionV1_1_AllowsPlotTikz()
    {
        var state = new State();
        state.Add("f", Unipi.Nancy.Expressions.Expressions.FromCurve(
            new Unipi.Nancy.NetworkCalculus.RateLatencyServiceCurve(1, 3), "f"));

        var statement = Statement.FromLine("plotTikz(f)", state, SyntaxVersion.V1_1);
        Assert.IsType<PlotTikzCommand>(statement);
    }

    [Fact]
    public void InteractiveMode_VersionV1_0_RejectsPlotTikz()
    {
        var state = new State();
        state.Add("f", Unipi.Nancy.Expressions.Expressions.FromCurve(
            new Unipi.Nancy.NetworkCalculus.RateLatencyServiceCurve(1, 3), "f"));

        Assert.ThrowsAny<Exception>(() => Statement.FromLine("plotTikz(f)", state, SyntaxVersion.V1_0));
    }

    [Fact]
    public void PreambleShebangV2_0_IsReported_AndStillParsesAsTheLatest()
    {
        const string programText = """
        #!syntax version 2.0
        a := 1
        printExpression(a)
        """;

        var program = Program.FromText(programText);

        var error = Assert.Single(program.Errors);
        Assert.Contains("is not supported by this build", error.Message);
        // the gating is unaffected, so what the script says about the constructs of this build stands
        Assert.Equal(new SyntaxVersion(2, 0), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is PrintExpressionCommand { VariableName: "a" });
    }

    [Fact]
    public void PreambleShebangV1_0_AllowsCoreConstructs()
    {
        const string programText = """
        #!syntax version 1.0
        a := 1 + 2
        b := ratency(1, 3)
        plot(b, xlim=[0, 10])
        a
        assert(a = 3)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
    }

    [Fact]
    public void ShebangInStatementPosition_ProducesWarning()
    {
        const string programText = """
        a := 1
        #!syntax version 1.0
        printExpression(a)
        """;

        var program = Program.FromText(programText);

        // No preamble → defaults to Latest, all constructs work
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        // Shebang on line 2 is in statement position → VersionDirectiveStatement with IsDuplicate
        var vds = program.Statements.OfType<VersionDirectiveStatement>().Single();
        Assert.True(vds.IsDuplicate);
        Assert.Contains(program.Statements, s => s is PrintExpressionCommand { VariableName: "a" });
    }

    [Fact]
    public void PreambleShebangV1_0_FullProgramWorks()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 3)
        g := affine(1, 0)
        h := f + g
        h(5)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.Equal(3, program.Statements.OfType<Assignment>().Count());
        Assert.Single(program.Statements.OfType<ExpressionCommand>());
    }

    [Fact]
    public void SyntaxVersionFromParts()
    {
        var v1_0 = SyntaxVersion.FromParts(1, 0);
        var v1_1 = SyntaxVersion.FromParts(1, 1);
        var v2_0 = SyntaxVersion.FromParts(2, 0);

        Assert.True(v1_0 < v1_1);
        Assert.True(v1_1 > v1_0);
        Assert.True(v1_1 >= v1_0);
        Assert.True(v2_0 >= v1_1);
        Assert.Equal("1.0", v1_0.ToString());
        Assert.Equal("1.1", v1_1.ToString());
    }

    [Fact]
    public void TryParseShebang_ValidFormats()
    {
        Assert.True(SyntaxVersion.TryParseShebang("#!syntax version 1.0", out var v1));
        Assert.Equal(new SyntaxVersion(1, 0), v1);

        Assert.True(SyntaxVersion.TryParseShebang("#!syntax  version  2.5", out var v2));
        Assert.Equal(new SyntaxVersion(2, 5), v2);

        Assert.True(SyntaxVersion.TryParseShebang("#!syntax version 1.1 extra trailing text", out var v3));
        Assert.Equal(new SyntaxVersion(1, 1), v3);
    }

    [Fact]
    public void TryParseShebang_InvalidFormats()
    {
        Assert.False(SyntaxVersion.TryParseShebang("# not a shebang", out _));
        Assert.False(SyntaxVersion.TryParseShebang("#!syntax 1.0", out _));
        Assert.False(SyntaxVersion.TryParseShebang("#!syntax version 1", out _));
        Assert.False(SyntaxVersion.TryParseShebang("", out _));
    }

    [Theory]
    [InlineData(
        """
        #!syntax version 1.0
        x := 1
        """,
        "1.0")]
    [InlineData(
        """
        x := 1
        """,
        "1.3")]
    public void ProgramSyntaxVersion_MatchesDeclaredVersion(string programText, string expectedVersion)
    {
        var program = Program.FromText(programText);
        Assert.Equal(expectedVersion, program.SyntaxVersion.ToString());
    }

    [Fact]
    public void MultipleShebangs_PreambleThenStatement_SecondIsWarning()
    {
        const string programText = """
        #!syntax version 1.0
        a := 1
        #!syntax version 1.1
        b := 2
        """;

        var program = Program.FromText(programText);

        // Preamble shebang sets version to 1.0
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        // Statement-level shebang produces warning
        var vds = program.Statements.OfType<VersionDirectiveStatement>().Single();
        Assert.True(vds.IsDuplicate);
        Assert.Equal(2, program.Statements.OfType<Assignment>().Count());
    }

    /// <summary>
    /// A second directive at the top of the file is as ineffective as one written after a statement, and is reported the same way.
    /// </summary>
    [Fact]
    public void SecondShebangOfThePreamble_ProducesWarning()
    {
        const string programText = """
        #!syntax version 1.0
        #!syntax version 1.1
        a := 1
        """;

        var program = Program.FromText(programText);

        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        var vds = Assert.Single(program.Statements.OfType<VersionDirectiveStatement>());
        Assert.True(vds.IsDuplicate);
        Assert.Equal(new SyntaxVersion(1, 1), vds.Version);
    }

    /// <summary>
    /// The warning names the version the program is read with, which is not the one the directive declares.
    /// </summary>
    [Theory]
    // the version of the directive that opens the program
    [InlineData("#!syntax version 1.0\n#!syntax version 1.1\na := 1", "1.0")]
    // the default, no directive having been applied
    [InlineData("a := 1\n#!syntax version 1.0", "1.3")]
    public void ShebangNotApplied_WarningNamesTheVersionInForce(string programText, string inForce)
    {
        var program = Program.FromText(programText);

        var vds = Assert.Single(program.Statements.OfType<VersionDirectiveStatement>());
        var warning = vds.Execute(new State());

        Assert.Contains("is not applied", warning);
        Assert.Contains($"Active version: {inForce}.", warning);
    }

    [Fact]
    public void MultipleShebangs_AllInStatementPosition_AllAreWarnings()
    {
        const string programText = """
        a := 1
        #!syntax version 1.0
        #!syntax version 1.1
        b := 2
        """;

        var program = Program.FromText(programText);

        // No preamble → version defaults to Latest
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        // All statement-level version directives are duplicates (only preamble is valid)
        var vdsList = program.Statements.OfType<VersionDirectiveStatement>().ToList();
        Assert.Equal(2, vdsList.Count);
        Assert.All(vdsList, v => Assert.True(v.IsDuplicate));
        Assert.Equal(2, program.Statements.OfType<Assignment>().Count());
    }

    [Fact]
    public void InteractiveMode_FirstVersionDirective_IsNotDuplicate()
    {
        var statement = Statement.FromLine("#!syntax version 1.0");

        var vds = Assert.IsType<VersionDirectiveStatement>(statement);
        Assert.False(vds.IsDuplicate);
        Assert.Equal(new SyntaxVersion(1, 0), vds.Version);
    }

    [Fact]
    public void InteractiveMode_VersionDirective_SetsParserVersion()
    {
        // Simulate interactive mode: first line sets version, next line uses it
        var state = new State();

        // Line 1: set version to 1.0
        var vds = Statement.FromLine("#!syntax version 1.0");
        var version = Assert.IsType<VersionDirectiveStatement>(vds).Version!.Value;

        // Line 2: with version 1.0, printExpression should throw (predicate fails)
        Assert.ThrowsAny<Exception>(() => Statement.FromLine("printExpression(x)", state, version));
    }

    [Fact]
    public void InteractiveMode_VersionV1_1_AllowsPrintExpression()
    {
        var state = new State();
        state.Add("x", Unipi.Nancy.Expressions.Expressions.FromRational(1, "x"));

        var statement = Statement.FromLine("printExpression(x)", state, SyntaxVersion.V1_1);
        Assert.IsType<PrintExpressionCommand>(statement);
    }

    [Fact]
    public void InteractiveMode_VersionV1_0_RejectsPrintExpression()
    {
        var state = new State();
        state.Add("x", Unipi.Nancy.Expressions.Expressions.FromRational(1, "x"));

        Assert.ThrowsAny<Exception>(() => Statement.FromLine("printExpression(x)", state, SyntaxVersion.V1_0));
    }

    [Fact]
    public void NoShebang_AllowsSubaddClosure()
    {
        const string programText = """
        f := ratency(1, 2)
        subaddclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void PreambleShebangV1_0_RejectsSubaddClosure()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 2)
        subaddclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
    }

    [Fact]
    public void PreambleShebangV1_2_AllowsSubaddClosure()
    {
        const string programText = """
        #!syntax version 1.2
        f := ratency(1, 2)
        subaddclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 2), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void NoShebang_AllowsSuperaddClosure()
    {
        const string programText = """
        f := ratency(1, 2)
        superaddclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void PreambleShebangV1_0_RejectsSuperaddClosure()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 2)
        superaddclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
    }

    [Fact]
    public void PreambleShebangV1_2_AllowsSuperaddClosure()
    {
        const string programText = """
        #!syntax version 1.2
        f := ratency(1, 2)
        superaddclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 2), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void NoShebang_AllowsLowClosure()
    {
        const string programText = """
        f := ratency(1, 2)
        lowclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void PreambleShebangV1_0_RejectsLowClosure()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 2)
        lowclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
    }

    [Fact]
    public void PreambleShebangV1_2_AllowsLowClosure()
    {
        const string programText = """
        #!syntax version 1.2
        f := ratency(1, 2)
        lowclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 2), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void NoShebang_AllowsNnLowClosure()
    {
        const string programText = """
        f := ratency(1, 2)
        nnlowclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }

    [Fact]
    public void PreambleShebangV1_0_RejectsNnLowClosure()
    {
        const string programText = """
        #!syntax version 1.0
        f := ratency(1, 2)
        nnlowclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 0), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
    }

    [Fact]
    public void PreambleShebangV1_2_AllowsNnLowClosure()
    {
        const string programText = """
        #!syntax version 1.2
        f := ratency(1, 2)
        nnlowclosure(f)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 2), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is ExpressionCommand);
    }
    // Keywords introduced after 1.0 must not act as keywords in scripts declaring an earlier version,
    // otherwise adding one to the syntax breaks existing scripts using that name as a variable.
    // Driven by VersionedKeywords.IntroducedIn, so keywords added later are covered without editing tests.

    public static IEnumerable<object[]> VersionedKeywordCases =>
        VersionedKeywords.IntroducedIn.Keys.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void VersionV1_0_AllowsLaterKeywordAsNumberVariable(string keyword)
    {
        var programText = $"""
        #!syntax version 1.0
        {keyword} := 3
        {keyword} + 1
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        var output = program.ExecuteToStringOutput().ToList();
        Assert.Contains("4", output);
    }

    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void VersionV1_0_AllowsLaterKeywordAsCurveVariable(string keyword)
    {
        // exercises the lookahead that routes function expressions, which matches on token text
        var programText = $"""
        #!syntax version 1.0
        {keyword} := ratency(1, 2)
        h := {keyword} * {keyword}
        h(10)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        var output = program.ExecuteToStringOutput().ToList();
        Assert.Contains("6", output);
    }

    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void VersionThatIntroducedIt_RejectsKeywordAsVariable(string keyword)
    {
        var introducedIn = VersionedKeywords.IntroducedIn[keyword];
        var programText = $"""
        #!syntax version {introducedIn}
        {keyword} := 3
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
    }

    // A script written before a keyword existed fails on a name it was free to use, so the error has to
    // say which name that is and how to keep it, rather than only where the parse gave up.
    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void KeywordUsedAsVariable_ErrorNamesItAndTheDirectiveToDeclare(string keyword)
    {
        var introducedIn = VersionedKeywords.IntroducedIn[keyword];
        var keepsIt = introducedIn.Previous();
        Assert.NotNull(keepsIt);

        var program = Program.FromText($"{keyword} := 3");

        var error = Assert.Single(program.Errors, e => e.Hint is not null);
        Assert.Contains($"'{keyword}'", error.Hint!);
        Assert.Contains($"version {introducedIn}", error.Hint!);
        Assert.Contains($"#!syntax version {keepsIt}", error.Hint!);
        // and following the hint makes the program parse
        Assert.Empty(Program.FromText($"#!syntax version {keepsIt}\n{keyword} := 3").Errors);
    }

    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void KeywordUsedAsVariable_InteractiveErrorCarriesTheSameHint(string keyword)
    {
        var introducedIn = VersionedKeywords.IntroducedIn[keyword];

        var exception = Assert.ThrowsAny<Exception>(
            () => Statement.FromLine($"{keyword} := 3", new State(), introducedIn));

        var error = Assert.IsType<SyntaxErrorException>(exception).Error;
        Assert.NotNull(error);
        Assert.Contains($"'{keyword}'", error.Hint!);
        Assert.Contains($"#!syntax version {introducedIn.Previous()}", error.Hint!);
    }

    // A line that starts with something no statement can start with used to be read as an empty
    // statement, because the empty alternative matches without consuming anything.
    [Theory]
    [InlineData("mod := 3")]
    [InlineData("mod 3")]
    [InlineData(") := 3")]
    public void InteractiveMode_LineThatNoStatementStartsWith_IsRejected(string line)
    {
        Assert.ThrowsAny<Exception>(() => Statement.FromLine(line, new State(), SyntaxVersion.Latest));
    }

    [Fact]
    public void InteractiveMode_InlineCommentIsStillAccepted()
    {
        var statement = Statement.FromLine("x := 1 // a comment", new State(), SyntaxVersion.Latest);

        Assert.IsType<Assignment>(statement);
    }

    // The hint is about a name used where the syntax expects an operator or a command: a call of the
    // keyword that fails to parse for another reason is not that case.
    [Fact]
    public void KeywordUsedAsOperator_ErrorCarriesNoHint()
    {
        var program = Program.FromText("f := affine(1, 0)\nfloor(f");

        Assert.NotEmpty(program.Errors);
        Assert.All(program.Errors, error => Assert.Null(error.Hint));
    }

    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void VersionedKeywordIsAKeywordOfTheGrammar(string keyword)
    {
        // guards against a stale or misspelled entry, which would silently gate nothing
        Assert.True(
            KeywordLexing.KeywordTokenTypes().ContainsKey(keyword),
            $"'{keyword}' is listed in VersionedKeywords.IntroducedIn but is not a keyword of the grammar.");
    }

    [Fact]
    public void EveryVersionGatedLexerRuleHasAKeywordEntry()
    {
        // A keyword is gated by a semantic predicate on its lexer rule, for which ANTLR generates a
        // <RULE>_sempred method. Every such keyword must also be listed in VersionedKeywords.IntroducedIn,
        // or the tests above would silently not cover it.
        const string suffix = "_sempred";
        var lexerType = typeof(Unipi.MppgParser.Grammar.MppgLexer);

        var gatedRules = lexerType
            .GetMethods(System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Public)
            .Select(method => method.Name)
            .Where(name => name.EndsWith(suffix, StringComparison.Ordinal))
            .Select(name => name[..^suffix.Length])
            .Distinct()
            .ToList();

        Assert.NotEmpty(gatedRules);

        var missing = gatedRules
            .Where(rule => KeywordOfLexerRule(rule) is not { } keyword
                           || !VersionedKeywords.IntroducedIn.ContainsKey(keyword))
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"The lexer rule(s) {string.Join(", ", missing)} are gated by syntax version, but the keyword "
            + "they match is not listed in VersionedKeywords.IntroducedIn. Add it, so that the version "
            + "tests cover it and scripts declaring an earlier version keep using the name as a variable.");
    }

    /// <summary>
    /// The keyword a lexer rule matches, or null if it does not match a single literal.
    /// </summary>
    private static string? KeywordOfLexerRule(string ruleName)
    {
        var tokenTypeField = typeof(Unipi.MppgParser.Grammar.MppgLexer)
            .GetField(ruleName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (tokenTypeField?.GetRawConstantValue() is not int tokenType)
            return null;

        var literal = Unipi.MppgParser.Grammar.MppgLexer.DefaultVocabulary.GetLiteralName(tokenType);
        return literal is null || literal.Length < 2 ? null : literal[1..^1];
    }
    // The lexer DFA is cached in a static field, shared by every lexer of the process. If ANTLR cached an
    // edge whose computation evaluated a version predicate, a parse would inherit the version of a previous
    // one, which would only show up as an order-dependent failure.
    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void GatingIsNotLeakedBetweenParsesOfDifferentVersions(string keyword)
    {
        var introducedIn = VersionedKeywords.IntroducedIn[keyword];

        var asKeyword = $"""
        #!syntax version {introducedIn}
        {keyword} := 3
        """;
        var asVariable = $"""
        #!syntax version 1.0
        {keyword} := 3
        """;

        // keyword first, then variable
        Assert.NotEmpty(Program.FromText(asKeyword).Errors);
        Assert.Empty(Program.FromText(asVariable).Errors);

        // and the other way round
        Assert.Empty(Program.FromText(asVariable).Errors);
        Assert.NotEmpty(Program.FromText(asKeyword).Errors);
    }

    // Interactive mode parses one line per lexer, so the directive typed earlier in the session is not in
    // the input being lexed: the version has to be passed in.
    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void InteractiveMode_VersionV1_0_AllowsLaterKeywordAsVariable(string keyword)
    {
        var statement = Statement.FromLine($"{keyword} := 3", new State(), SyntaxVersion.V1_0);

        Assert.IsType<Assignment>(statement);
    }

    [Theory]
    [MemberData(nameof(VersionedKeywordCases))]
    public void InteractiveMode_VersionThatIntroducedIt_RejectsKeywordAsVariable(string keyword)
    {
        var introducedIn = VersionedKeywords.IntroducedIn[keyword];

        Assert.ThrowsAny<Exception>(
            () => Statement.FromLine($"{keyword} := 3", new State(), introducedIn));
    }

    // Applying a directive does not execute a statement, so it is not in the statement history:
    // without putting it back, an exported session would run again at a different version.
    [Fact]
    public void SessionProgramLines_KeepTheAppliedVersionDirective()
    {
        var programContext = new ProgramContext
        {
            SyntaxVersion = SyntaxVersion.V1_0,
            SyntaxVersionDirectiveApplied = true
        };
        programContext.StatementHistory.Add(
            Statement.FromLine("lowclosure := 3", programContext.State, SyntaxVersion.V1_0));

        var lines = programContext.ToProgramLines().ToList();

        Assert.Equal("#!syntax version 1.0", lines[0]);
        // and the exported program parses back the same way
        var reparsed = Program.FromText(string.Join("\n", lines));
        Assert.Empty(reparsed.Errors);
        Assert.Equal(SyntaxVersion.V1_0, reparsed.SyntaxVersion);
    }

    [Fact]
    public void SessionProgramLines_OmitTheDirectiveWhenNoneWasApplied()
    {
        var programContext = new ProgramContext();
        programContext.StatementHistory.Add(
            Statement.FromLine("a := 3", programContext.State));

        var lines = programContext.ToProgramLines().ToList();

        Assert.Equal(["a := 3"], lines);
    }
    // Built by concatenation on purpose: a raw string literal would have its indentation stripped by the
    // compiler, putting the directive back at column 0 and hiding what these cover.

    [Fact]
    public void IndentedPreambleDirective_IsApplied()
    {
        var programText = "  #!syntax version 1.0\n  lowclosure := 3\n  lowclosure + 1";

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.V1_0, program.SyntaxVersion);
        Assert.Contains("4", program.ExecuteToStringOutput().ToList());
    }

    [Fact]
    public void IndentedPreambleDirective_WithTrailingBlanks_IsApplied()
    {
        // the directive's own trailing blanks used to be what the check read, making it intermittent
        var programText = "  #!syntax version 1.0   \n  lowclosure := 3";

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.V1_0, program.SyntaxVersion);
    }

    [Fact]
    public void DirectiveAfterABlankLine_IsNotApplied()
    {
        // only the preamble counts, and a blank line is already a statement
        var programText = "\n#!syntax version 1.0\na := 1";

        var program = Program.FromText(programText);

        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.True(program.Statements.OfType<VersionDirectiveStatement>().Single().IsDuplicate);
    }

    // Regression test: the shebang and a following plain comment used to lex as the same token
    // type (INLINABLE_COMMENT), so the parser needed a content-dependent predicate to tell them
    // apart when deciding whether to keep parsing the preamble. That predicate was evaluated
    // against the wrong lookahead position during ANTLR's adaptive prediction of the preamble's
    // loop, so it optimistically kept parsing the preamble, then failed for real on the comment,
    // throwing "rule versionDirective failed predicate". Giving '#!' its own token type
    // (DIRECTIVE_START / VERSION_DIRECTIVE_START) removes the need for that predicate entirely.
    [Fact]
    public void ShebangImmediatelyFollowedByComment_ParsesWithoutError()
    {
        const string programText = """
        #!syntax version 1.2
        // a comment
        x := 5
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(new SyntaxVersion(1, 2), program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is Comment);
        Assert.Contains(program.Statements, s => s is Assignment { VariableName: "x" });
    }

    [Fact]
    public void UnknownDirective_InPreamblePosition_DoesNotAffectVersionOrError()
    {
        const string programText = """
        #!some-future-directive
        a := 1
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        Assert.Equal(SyntaxVersion.Latest, program.SyntaxVersion);
        Assert.Contains(program.Statements, s => s is Assignment { VariableName: "a" });
    }

    [Fact]
    public void UnknownDirective_InStatementPosition_ProducesDirectiveStatement()
    {
        const string programText = """
        a := 1
        #!some-future-directive
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
        var directive = program.Statements.OfType<DirectiveStatement>().Single();
        Assert.Equal("#!some-future-directive", directive.Text);
    }

    /// <summary>
    /// A version later than the latest gates the syntax as the latest, every predicate of the grammar
    /// being an "or later" one, so a script that declares one and parses needs nothing beyond it.
    /// </summary>
    [Fact]
    public void VersionLaterThanLatest_IsReported_AndSaysTheLatestWouldDo()
    {
        var program = Program.FromText($"#!syntax version 1.{SyntaxVersion.Latest.Minor + 1}\na := 1");

        var error = Assert.Single(program.Errors);
        Assert.Equal(1, error.Line);
        Assert.Contains("is not supported by this build", error.Message);
        Assert.Contains($"can declare '#!syntax version {SyntaxVersion.Latest}'", error.Hint);
    }

    [Fact]
    public void VersionLaterThanLatest_WithOtherErrors_SaysTheyMayBeItsConstructs()
    {
        var program = Program.FromText($"#!syntax version 1.{SyntaxVersion.Latest.Minor + 1}\na := 1\nb := ]");

        // the version comes first, being the cause of the errors a wrong gating produces
        Assert.Contains("is not supported by this build", program.Errors[0].Message);
        Assert.Contains("may be constructs of", program.Errors[0].Hint);
        Assert.True(program.Errors.Count > 1);
    }

    [Fact]
    public void VersionThatNeverExisted_IsReportedWithTheKnownOnes()
    {
        var program = Program.FromText("#!syntax version 0.9\na := 1");

        var error = program.Errors[0];
        Assert.Contains("0.9 is not a known version", error.Message);
        Assert.Contains(SyntaxVersion.Latest.ToString(), error.Message);
    }

    [Fact]
    public void MalformedVersionDirective_IsReportedRatherThanIgnored()
    {
        var program = Program.FromText("#!syntax version 1.x\na := 1");

        var error = Assert.Single(program.Errors);
        Assert.Contains("is not a version directive", error.Message);
    }

    [Fact]
    public void MalformedVersionDirective_DeclaresNoVersionToApply()
    {
        var statement = Assert.IsType<VersionDirectiveStatement>(Statement.FromLine("#!syntax version 1.x"));

        Assert.Null(statement.Version);
        Assert.NotNull(statement.Error);
    }
}
