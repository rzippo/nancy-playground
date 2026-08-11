using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class ProgramExecution
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ProgramExecution(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<string> Programs =
    [
        // The two scripts below spell the floor curve as a variable named 'floor', which is a keyword
        // from syntax version 1.3 on: they declare 1.2 to keep the name, as any script written before
        // the operator existed can.
        """
        #!syntax version 1.2
        T4 := 60
        A1 := stair(0, 60, 35)
        A2 := stair (0, 5, 2)
        A4 := stair (0, T4, 12)
        C := affine (1 ,0)
        D1 := C + (A1 - C)*zero
        D2 := C + (A1 + A2 - C)*zero - D1
        D4 := C + (A4 - C)*zero
        floor := right-ext(stair(1, 1, 1))
        A3 := ( floor comp (D2 / 2) ) * 4
        D3 := C + (A3 + A4 - C)*zero - D4
        hDev(A3 , D3)
        """,
        """
        #!syntax version 1.2
        T4 := 60
        A1 := stair(0, 60, 35)
        A2 := stair (0, 5, 2)
        A4 := stair (0, T4, 12)
        C := affine (1 ,0)
        D1 := C + (A1 - C)*zero
        D2 := C + (A1 + A2 - C)*zero - D1
        D4 := C + (A4 - C)*zero
        floor := right-ext(stair(1, 1, 1))
        A3 := ( floor comp (D2 / 2) ) * 4
        D3 := C + (A3 + A4 - C)*zero - D4
        h := hDev(A3 , D3)
        printExpression(h)
        h
        """,
        // the same script written with the floor operator of 1.3, which replaces the curve above
        """
        T4 := 60
        A1 := stair(0, 60, 35)
        A2 := stair (0, 5, 2)
        A4 := stair (0, T4, 12)
        C := affine (1 ,0)
        D1 := C + (A1 - C)*zero
        D2 := C + (A1 + A2 - C)*zero - D1
        D4 := C + (A4 - C)*zero
        A3 := floor( D2 / 2 ) * 4
        D3 := C + (A3 + A4 - C)*zero - D4
        hDev(A3 , D3)
        """
    ];

    public static IEnumerable<object[]> ProgramTestCases =
        Programs.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ProgramTestCases))]
    public void ProgramExecutionToStringOutput(string programText)
    {
        var program = Program.FromText(programText);
        var output = program.ExecuteToStringOutput();
        foreach (var line in output)
        {
            _testOutputHelper.WriteLine(line);
        }
    }

    [Theory]
    [InlineData("g := ratency(1,1) + 1")]
    [InlineData("g := 1 + ratency(1,1)")]
    public void ScalarAndCurveConstructorAssignmentsAreValidPrograms(string programText)
    {
        var program = Program.FromText(programText);

        Assert.Empty(program.Errors);

        _ = program.ExecuteToStringOutput().ToList();
        var (_, type) = program.ProgramContext.State.GetVariableType("g");
        Assert.Equal(ExpressionType.Function, type);
    }
}
