namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// A scalar chain in front of a function, as in <c>10 * b * c</c>, was rejected at the function operand, where <c>b * 10 * c</c> and <c>(10 * b) * c</c> parsed.
/// The parse tree of the rejected form held <c>10 * b</c> as a <c>numberProductExpression</c>, the number chain having taken the operator and the function operand with it.
/// </summary>
/// <remarks>
/// These cases pin that the forms parse, not how they group.
/// Which operand a scalar scales is what <c>printExpression</c> discloses, covered in <see cref="MppgClassicOutputTests"/>.
/// </remarks>
public class ScalarFunctionProductChainParsing
{
    /// <summary>
    /// <c>*</c> is the token that carries the ambiguity, being both the number chain's own operator and the scalar-times-function one, so the cases vary what surrounds it.
    /// <c>comp</c> is here for company, the number chain never continuing over it.
    /// </summary>
    public static List<string> Chains =>
    [
        "10 * b * c",
        "b * 10 * c",
        "(10 * b) * c",
        "1/2 * b * c",
        "2 * 3 * b * c",
        "10 / 2 * b * c",
        "10 div 3 * b * c",
        "10 mod 3 * b * c",
        "10 * b * c * d",
        "10 * -b * c",
        "-10 * b * c",
        "-1/2 * b * c",
        "10 comp b comp c",
        "2 * 3 comp b comp c",
        "10 * n comp b",
        "n * n comp b",
        "10 * b * c + d",
        "d + 10 * b * c",
    ];

    public static IEnumerable<object[]> ChainTestCases =>
        Chains.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ChainTestCases))]
    public void AScalarChainInFrontOfAFunctionParses(string expression)
    {
        var program = Program.FromText($"b := bucket(2, 5)\nc := bucket(3, 7)\nd := bucket(1, 1)\nn := 4\n{expression}");

        Assert.Empty(program.Errors);
    }
}
