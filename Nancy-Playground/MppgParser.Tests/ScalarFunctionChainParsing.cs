namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// A scalar chain in front of a function was rejected at the function operand, as <c>10 * b * c</c> was at <c>b</c> and <c>10 + 10 + b</c> at <c>b</c>, where the same expressions parsed with brackets.
/// The number chain reached the operator first and took the function operand with it, leaving nothing for the alternative that reads a scalar against a function.
/// </summary>
/// <remarks>
/// These cases pin that the forms parse, not how they group.
/// Which operand a scalar meets is what <c>printExpression</c> discloses, covered in <see cref="MppgClassicOutputTests"/>.
/// </remarks>
public class ScalarFunctionChainParsing
{
    /// <summary>
    /// The product tier, where <c>*</c> is both the number chain's own operator and the scalar-times-function one.
    /// The cases vary what surrounds it: the scalar side, the function chain after it, signs, brackets, and the number kinds a scalar can be spelled with.
    /// <c>comp</c> and the scalar-over-curve <c>/</c> ride along, the number chain never continuing over the first and stopping before the curve on the second.
    /// </summary>
    public static List<string> ProductChains =>
    [
        "10 * b",
        "10 * b * c",
        "b * 10 * c",
        "(10 * b) * c",
        "10 * b * c * d",
        "1/2 * b * c",
        "2 * 3 * b * c",
        "10 / 2 * b * c",
        "10 div 3 * b * c",
        "10 mod 3 * b * c",
        "10 * n * b",
        "10 * -n * b",
        "10 * -5 * b",
        "10 * (1 + 2) * b",
        "10 * -b * c",
        "-10 * b * c",
        "-1/2 * b * c",
        "10 / b",
        "10 / b / c",
        "2 * 5 / b",
        "1/2 / b",
        "10 comp b",
        "10 comp b comp c",
        "2 * 3 comp b comp c",
        "10 * n comp b",
        "n * n comp b",
        "10 comp 10",
        "10 comp 10 * b",
        "10 comp n comp b",
    ];

    /// <summary>
    /// The sum tier, where the scalar side spans a chain of its own: in <c>1 + 2 + f</c> it is <c>1 + 2</c>.
    /// Each of the four sum operators appears on both sides of the handover.
    /// </summary>
    public static List<string> SumChains =>
    [
        "10 + b",
        "10 + 10 + b",
        "1 + 2 + 3 + b",
        "10 - 10 - b",
        "10 /\\ 10 /\\ b",
        "10 \\/ n \\/ c",
        "n - 10 /\\ b",
        "10 + n \\/ c",
        "10 + -n + b",
        "1/2 + 1/2 + b",
        "10 * 2 + b",
        "b + 10 + 10",
        "b + 10 comp 10",
    ];

    /// <summary>
    /// The two tiers meeting, the product chain standing as an operand of the sum.
    /// </summary>
    public static List<string> MixedTierChains =>
    [
        "10 + 10 + b * c",
        "10 * b * c + d",
        "d + 10 * b * c",
    ];

    public static IEnumerable<object[]> ChainTestCases =>
        ProductChains.Concat(SumChains).Concat(MixedTierChains).ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ChainTestCases))]
    public void AScalarChainInFrontOfAFunctionParses(string expression)
    {
        var program = Program.FromText($"b := bucket(2, 5)\nc := bucket(3, 7)\nd := bucket(1, 1)\nn := 4\n{expression}");

        Assert.Empty(program.Errors);
    }
}
