using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class ParseRecovery
{
    [Fact]
    public void RecoveredMalformedStatementIsRepresentedAsSyntaxErrorStatement()
    {
        const string programText = """
        a := 1
        1 = 2
        b := 2
        printExpression(b)
        """;

        var program = Program.FromText(programText);

        Assert.NotEmpty(program.Errors);
        Assert.Contains(program.Statements, s => s is SyntaxErrorStatement);
        Assert.Contains(program.Statements, s => s is Assignment { VariableName: "b" });
        Assert.Contains(program.Statements, s => s is PrintExpressionCommand { VariableName: "b" });
    }
}
