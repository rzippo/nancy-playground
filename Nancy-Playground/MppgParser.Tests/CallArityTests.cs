namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The arities a message quotes, held to what the grammar accepts.
/// </summary>
/// <remarks>
/// The table is written down rather than read from the grammar, so a call written with the number it claims has to parse, and the same call with one argument more has to fail.
/// A rule that gains or loses an argument then fails here rather than quietly making every message about it wrong.
/// </remarks>
public class CallArityTests
{
    /// <summary>
    /// A call of each name, written with the arguments of the kinds it takes.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> Arguments = new Dictionary<string, string[]>
    {
        ["ratency"] = ["1", "2"],
        ["bucket"] = ["1", "2"],
        ["affine"] = ["1", "2"],
        ["step"] = ["1", "2"],
        ["stair"] = ["1", "2", "3"],
        ["delay"] = ["1"],
        ["star"] = ["f"],
        ["subaddclosure"] = ["f"],
        ["superaddclosure"] = ["f"],
        ["hShift"] = ["f", "1"],
        ["hshift"] = ["f", "1"],
        ["vShift"] = ["f", "1"],
        ["vshift"] = ["f", "1"],
        ["inv"] = ["f"],
        ["low_inv"] = ["f"],
        ["up_inv"] = ["f"],
        ["upclosure"] = ["f"],
        ["nnupclosure"] = ["f"],
        ["lowclosure"] = ["f"],
        ["nnlowclosure"] = ["f"],
        ["left-ext"] = ["f"],
        ["right-ext"] = ["f"],
        ["hDev"] = ["f", "f"],
        ["hdev"] = ["f", "f"],
        ["vDev"] = ["f", "f"],
        ["vdev"] = ["f", "f"],
        ["zDev"] = ["f", "f"],
        ["zdev"] = ["f", "f"],
        ["floor"] = ["f"],
        ["ceil"] = ["f"],
        ["abs"] = ["1"],
        ["pow"] = ["2", "3"],
        ["gcd"] = ["4", "6"],
        ["lcm"] = ["4", "6"]
    };

    public static TheoryData<string> Names => [.. CallArity.All.Select(entry => entry.Key)];

    private static string Program(string call, IEnumerable<string> arguments)
        => $"f := bucket(2, 5)\ng := {call}({string.Join(", ", arguments)})";

    /// <summary>
    /// Every name in the table has a call written for it here, so none of them goes unchecked.
    /// </summary>
    [Fact]
    public void EveryNameIsExercised()
    {
        Assert.All(CallArity.All, entry =>
        {
            Assert.True(Arguments.ContainsKey(entry.Key), $"'{entry.Key}' has no call written for it");
            Assert.Equal(entry.Value, Arguments[entry.Key].Length);
        });
    }

    /// <summary>
    /// The number the table claims is the number the grammar takes.
    /// </summary>
    [Theory]
    [MemberData(nameof(Names))]
    internal void ACallOfThatLengthParses(string call)
    {
        var program = MppgParser.Program.FromText(Program(call, Arguments[call]));

        Assert.Empty(program.Errors.Select(error => error.ToString(verbose: true)));
    }

    /// <summary>
    /// One argument more does not, which is what makes the number above the whole of what the call takes.
    /// </summary>
    [Theory]
    [MemberData(nameof(Names))]
    internal void OneArgumentMoreDoesNot(string call)
    {
        var program = MppgParser.Program.FromText(Program(call, Arguments[call].Append(Arguments[call][^1])));

        Assert.NotEmpty(program.Errors);
    }
}
