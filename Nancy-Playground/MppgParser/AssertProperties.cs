using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// The properties <c>assert(f is subadditive)</c> can test, and the Nancy member behind each.
/// </summary>
/// <remarks>
/// One list drives the grammar's contextual keyword check, run-mode evaluation and every convert
/// visitor, so a property added here needs no other registration.
/// </remarks>
public static class AssertProperties
{
    /// <summary>
    /// Whether a property applies to a function operand, a number operand, or either.
    /// </summary>
    public enum Applicability
    {
        /// <summary>Function operands only.</summary>
        Function,
        /// <summary>Number operands only.</summary>
        Scalar,
        /// <summary>Either a function or a number operand.</summary>
        Both
    }

    /// <summary>
    /// One property: its canonical spelling, the boolean member of <c>Curve</c>/<c>Rational</c> it
    /// reads (the same name on both, where <see cref="Applicability"/> is <see cref="Applicability.Both"/>),
    /// and which operand kinds it accepts.
    /// </summary>
    public sealed record Definition(string CanonicalName, string NancyMember, Applicability Applicability);

    private static readonly Definition[] Definitions =
    [
        // Network-calculus algebra.
        new("subadditive", "IsSubAdditive", Applicability.Function),
        new("superadditive", "IsSuperAdditive", Applicability.Function),
        new("concave", "IsConcave", Applicability.Function),
        new("convex", "IsConvex", Applicability.Function),
        // Monotonicity and shape.
        new("nondecreasing", "IsNonDecreasing", Applicability.Function),
        new("increasing", "IsIncreasing", Applicability.Function),
        new("plain", "IsPlain", Applicability.Function),
        new("ultimatelyplain", "IsUltimatelyPlain", Applicability.Function),
        new("ultimatelyaffine", "IsUltimatelyAffine", Applicability.Function),
        new("ultimatelyconstant", "IsUltimatelyConstant", Applicability.Function),
        // Continuity.
        new("continuous", "IsContinuous", Applicability.Function),
        new("leftcontinuous", "IsLeftContinuous", Applicability.Function),
        new("rightcontinuous", "IsRightContinuous", Applicability.Function),
        new("continuousexceptorigin", "IsContinuousExceptOrigin", Applicability.Function),
        new("passingthroughorigin", "IsPassingThroughOrigin", Applicability.Function),
        // Sign and finiteness, function-only.
        new("nonnegative", "IsNonNegative", Applicability.Function),
        new("ultimatelyfinite", "IsUltimatelyFinite", Applicability.Function),
        new("ultimatelyplusinfinite", "IsUltimatelyPlusInfinite", Applicability.Function),
        new("ultimatelyminusinfinite", "IsUltimatelyMinusInfinite", Applicability.Function),
        new("ultimatelyinfinite", "IsUltimatelyInfinite", Applicability.Function),
        // Sign and finiteness, shared with Rational under the same member name.
        new("finite", "IsFinite", Applicability.Both),
        new("zero", "IsZero", Applicability.Both),
        new("plusinfinite", "IsPlusInfinite", Applicability.Both),
        new("minusinfinite", "IsMinusInfinite", Applicability.Both),
        // Scalar-only.
        new("integer", "IsInteger", Applicability.Scalar),
    ];

    /// <summary>
    /// Literature acronyms for a property, routing to the same <see cref="Definition"/>.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Synonyms =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ua"] = "ultimatelyaffine",
            ["uc"] = "ultimatelyconstant",
            ["ui"] = "ultimatelyinfinite",
        };

    /// <summary>
    /// Every accepted spelling, canonical and synonym alike, mapped to its <see cref="Definition"/>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Definition> ByName = BuildByName();

    private static IReadOnlyDictionary<string, Definition> BuildByName()
    {
        var byCanonical = Definitions.ToDictionary(definition => definition.CanonicalName, StringComparer.Ordinal);
        var result = new Dictionary<string, Definition>(byCanonical, StringComparer.Ordinal);
        foreach (var (synonym, canonical) in Synonyms)
            result[synonym] = byCanonical[canonical];
        return result;
    }

    /// <summary>
    /// True if <paramref name="text"/> is a property name or synonym, which is what the grammar's
    /// contextual keyword predicate checks.
    /// </summary>
    public static bool IsPropertyName(string text) => ByName.ContainsKey(text);

    /// <summary>
    /// The property <paramref name="name"/> resolves to, checked against <paramref name="operandType"/>.
    /// </summary>
    /// <exception cref="Exception">The property does not apply to that operand's kind.</exception>
    public static Definition Resolve(string name, ExpressionType operandType)
    {
        var definition = ByName[name];
        var applies = definition.Applicability switch
        {
            Applicability.Function => operandType == ExpressionType.Function,
            Applicability.Scalar => operandType == ExpressionType.Number,
            Applicability.Both => operandType is ExpressionType.Function or ExpressionType.Number,
            _ => false
        };
        if (!applies)
        {
            var operandKind = operandType == ExpressionType.Function ? "a function" : "a number";
            throw new Exception($"'{definition.CanonicalName}' does not apply to {operandKind}.");
        }

        return definition;
    }
}
