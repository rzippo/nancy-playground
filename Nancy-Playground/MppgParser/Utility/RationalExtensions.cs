using System.Text;
using System.Text.RegularExpressions;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Utility;

/// <summary>
/// Writes rationals as the generated C# code spells them.
/// </summary>
public static class RationalExtensions
{
    // todo: consider including these into Unipi.Nancy.Numerics

    /// <summary>
    /// Returns a string representing the explicit code to create the given Rational.
    /// It is explicit in the sense that it uses the Rational constructor, instead of implicit conversions from int.
    /// </summary>
    public static string ToExplicitCodeString(this Rational r)
    {
        if (r.IsPlusInfinite)
            return "Rational.PlusInfinity";
        if (r.IsMinusInfinite)
            return "Rational.MinusInfinity";

        var sb = new StringBuilder();
        sb.Append("new Rational(");
        sb.Append(SignedNumerator(r).ToString());
        if (r.Denominator != 1)
        {
            sb.Append(", ");
            sb.Append(r.Denominator.ToString());
        }
        sb.Append(")");

        return sb.ToString();
    }

    /// <summary>
    /// The bare integer code for the given Rational, e.g. "7" or "-7", when it is a whole number:
    /// safe wherever Rational's own implicit int conversion applies (an argument, an assignment
    /// target, an operand of +, -, or * against something already Rational). Null for a fraction or
    /// an infinity, which have no such bare form and keep their explicit one.
    /// </summary>
    public static string? ToBareIntCodeStringOrNull(this Rational r) =>
        r.IsFinite && r.Denominator == 1
            ? SignedNumerator(r).ToString()
            : null;

    private static System.Numerics.BigInteger SignedNumerator(Rational r)
    {
        var numerator = r.Numerator;
        return r.Sign < 0 && numerator > 0 ? -numerator : numerator;
    }

    /// <summary>
    /// Replaces the infinities of a generated expression with the named constants of Nancy.
    /// </summary>
    public static string UseNamedInfinityConstants(this string code) =>
        Regex.Replace(
            Regex.Replace(code, @"new Rational\(\s*1\s*,\s*0\s*\)", "Rational.PlusInfinity"),
            @"new Rational\(\s*-1\s*,\s*0\s*\)",
            "Rational.MinusInfinity");

    /// <summary>
    /// Returns a pretty string representation of the Rational.
    /// If the Rational is infinite, it returns "Infinity" or "-Infinity" instead of 1/0 or -1/0.
    /// </summary>
    public static string ToPrettyString(this Rational r)
    {
        if (r.IsFinite)
            return r.ToString();
        else
            return $"{(r.Sign == 1 ? '+' : '-')}Infinity";
    }
}
