namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// How many arguments each call of the syntax takes, so that a message can say the number rather than that one is missing.
/// </summary>
/// <remarks>
/// Read from the rules of <c>Mppg.g4</c>, and held to them by a test that writes each call with the number below and expects it to parse.
/// The calls whose length varies, i.e. <c>uaf</c> and <c>upp</c>, are left out: nothing here can say what they take.
/// </remarks>
internal static class CallArity
{
    private static readonly IReadOnlyDictionary<string, int> Arities = new Dictionary<string, int>
    {
        // the constructors
        ["ratency"] = 2,
        ["bucket"] = 2,
        ["affine"] = 2,
        ["step"] = 2,
        ["stair"] = 3,
        ["delay"] = 1,
        // the operations on a curve
        ["star"] = 1,
        ["subaddclosure"] = 1,
        ["superaddclosure"] = 1,
        ["hShift"] = 2,
        ["hshift"] = 2,
        ["vShift"] = 2,
        ["vshift"] = 2,
        ["inv"] = 1,
        ["low_inv"] = 1,
        ["up_inv"] = 1,
        ["upclosure"] = 1,
        ["nnupclosure"] = 1,
        ["lowclosure"] = 1,
        ["nnlowclosure"] = 1,
        ["upnoninc"] = 1,
        ["upnonincclosure"] = 1,
        ["lownoninc"] = 1,
        ["lownonincclosure"] = 1,
        ["upnondec"] = 1,
        ["upnondecclosure"] = 1,
        ["lownondec"] = 1,
        ["lownondecclosure"] = 1,
        ["nnupnondec"] = 1,
        ["nnupnondecclosure"] = 1,
        ["nnlownondec"] = 1,
        ["nnlownondecclosure"] = 1,
        ["left-ext"] = 1,
        ["right-ext"] = 1,
        // the ones that read a number off a curve
        ["hDev"] = 2,
        ["hdev"] = 2,
        ["vDev"] = 2,
        ["vdev"] = 2,
        ["zDev"] = 2,
        ["zdev"] = 2,
        // the ones on numbers, which take a curve or a number alike
        ["floor"] = 1,
        ["ceil"] = 1,
        ["abs"] = 1,
        ["pow"] = 2,
        ["gcd"] = 2,
        ["lcm"] = 2
    };

    /// <summary>
    /// The number of arguments <paramref name="call"/> takes, or null where it is not a call of a known length.
    /// </summary>
    public static int? Of(string? call)
        => call is not null && Arities.TryGetValue(call, out var arity) ? arity : null;

    /// <summary>
    /// The names above, for a test to hold each one to the grammar.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, int>> All => Arities;

    /// <summary>
    /// What a call of <paramref name="call"/> takes, said as a number: <c>stair</c> takes 3 arguments.
    /// </summary>
    public static string? Says(string? call)
        => Of(call) is not { } arity
            ? null
            : $"'{call}' takes {arity} argument{(arity == 1 ? "" : "s")}";
}
