using System.Globalization;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Numerics;

namespace Unipi.MppgParser.Visitors;

public partial class ExpressionVisitor : MppgBaseVisitor<IExpression>
{
    public State State { get; init; }

    public ExpressionVisitor(State? state)
    {
        State = state ?? new();
    }

    public override IExpression VisitFunctionVariableExp(Unipi.MppgParser.Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new Exception($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitNumberVariableExp(Grammar.MppgParser.NumberVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new Exception($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitNumberLiteralExp(Grammar.MppgParser.NumberLiteralExpContext context)
    {
        var numberText = context.GetText();
        Rational value;
        if (numberText.Contains("inf") || numberText.Contains("infinity") || numberText.Contains("Infinity"))
        {
            numberText = numberText.Replace("inf", string.Empty).Trim();
            if(numberText.Length == 0 || numberText == "+")
                value = Rational.PlusInfinity;
            else if (numberText == "-")
                value = Rational.MinusInfinity;
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

        var valueExp = Expressions.FromRational(value, "");
        return valueExp;
    }
}