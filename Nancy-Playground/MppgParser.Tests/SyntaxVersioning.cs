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
    public void PreambleShebangV2_0_AllowsPrintExpression()
    {
        const string programText = """
        #!syntax version 2.0
        a := 1
        printExpression(a)
        """;

        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);
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
        "1.2")]
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
        var version = Assert.IsType<VersionDirectiveStatement>(vds).Version;

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
}
