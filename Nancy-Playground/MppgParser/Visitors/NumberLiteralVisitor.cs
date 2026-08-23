using System.Globalization;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Reads a number literal as the rational it spells.
/// </summary>
public class NumberLiteralVisitor : MppgBaseVisitor<Rational>
{
    /// <summary>
    /// Reads a rational literal, i.e. one written as a fraction.
    /// </summary>
    public override Rational VisitRationalLiteral(Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext context)
    {
        var literals = context.numberLiteral();
        var numerator = VisitNumberLiteral(literals[0]);
        if (literals.Length == 1)
            return numerator;

        var denominator = VisitNumberLiteral(literals[1]);
        if (denominator.IsZero)
            throw new Exception($"Invalid rational literal, its denominator is zero: {context.GetText()}");

        return numerator / denominator;
    }

    /// <summary>
    /// Reads a number literal, whichever of the forms it is written in.
    /// </summary>
    public override Rational VisitNumberLiteral(Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        var numberText = context.GetText();
        Rational value;
        if (numberText.Contains("inf") || numberText.Contains("infinity") || numberText.Contains("Infinity"))
        {
            if (numberText[0] == '+')
                value = Rational.PlusInfinity;
            else if (numberText[0] == '-')
                value = Rational.MinusInfinity;
            else if (numberText[0] == 'i' || numberText[0] == 'I')
                // assume + is the default
                value = Rational.PlusInfinity;
            else
                throw new Exception($"Invalid number literal: {numberText}");
        }
        else if(numberText.Contains('.'))
        {
            value = decimal.Parse(numberText, CultureInfo.InvariantCulture);
        }
        else if (int.TryParse(numberText, NumberStyles.Number, CultureInfo.InvariantCulture, out var numerator))
        {
            value = numerator;
        }
        else
        {
            throw new Exception($"Invalid number literal: {numberText}");
        }

        return value;
    }
}