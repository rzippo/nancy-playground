using System.Globalization;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Numerics;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public class NumberLiteralVisitor : MppgBaseVisitor<Rational>
{
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
        else if(numberText.Contains('/'))
        {
            var split = numberText.Split("/");
            var numerator = int.Parse(split[0], CultureInfo.InvariantCulture);
            var denominator = int.Parse(split[1], CultureInfo.InvariantCulture);
            value = new Rational(numerator, denominator);
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