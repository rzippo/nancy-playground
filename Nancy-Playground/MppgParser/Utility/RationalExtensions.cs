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

        var numerator = r.Numerator;
        if (r.Sign < 0 && numerator > 0)
            numerator = -numerator;

        var sb = new StringBuilder();
        sb.Append("new Rational(");
        sb.Append(numerator.ToString());
        if (r.Denominator != 1)
        {
            sb.Append(", ");
            sb.Append(r.Denominator.ToString());
        }
        sb.Append(")");

        return sb.ToString();
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
