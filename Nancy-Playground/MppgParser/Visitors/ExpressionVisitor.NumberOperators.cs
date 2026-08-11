using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    public override IExpression VisitNumberMulDiv(Unipi.MppgParser.Grammar.MppgParser.NumberMulDivContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PROD_SIGN:
            {
                if (ilE is RationalExpression lRE && irE is RationalExpression rRE)
                    return RationalExpression.Product(lRE, rRE);

                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }

            case Unipi.MppgParser.Grammar.MppgParser.DIV_SIGN:
            case Unipi.MppgParser.Grammar.MppgParser.DIV_OP:
            {
                if (ilE is RationalExpression lRE && irE is RationalExpression rRE)
                    return RationalExpression.Division(lRE, rRE);

                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
            
            default: 
                throw new InvalidOperationException($"Unexpected operation: {operation.Text}");
        }
    }
    
    public override IExpression VisitNumberSumSubMinMax(Unipi.MppgParser.Grammar.MppgParser.NumberSumSubMinMaxContext context)
    {
        if (context.ChildCount != 3)
            throw new Exception("Expected 3 child expression");

        var ilE = context.GetChild(0).Accept(this);
        var irE = context.GetChild(2).Accept(this);
        var operation = context.op;

        switch (operation.Type)
        {
            case Unipi.MppgParser.Grammar.MppgParser.PLUS:
            {
                if (ilE is RationalExpression lRE && irE is RationalExpression rRE)
                    return RationalExpression.Addition(lRE, rRE);

                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }

            case Unipi.MppgParser.Grammar.MppgParser.MINUS:
            {
                if (ilE is RationalExpression lRE && irE is RationalExpression rRE)
                    return RationalExpression.Subtraction(lRE, rRE);

                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }

            case Unipi.MppgParser.Grammar.MppgParser.WEDGE:
            {
                if (ilE is RationalExpression lRE && irE is RationalExpression rRE)
                    return RationalExpression.Min(lRE, rRE);

                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }

            case Unipi.MppgParser.Grammar.MppgParser.VEE:
            {
                if (ilE is RationalExpression lRE && irE is RationalExpression rRE)
                    return RationalExpression.Max(lRE, rRE);

                throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");
            }
                
            default: 
                throw new InvalidOperationException($"Unexpected operation: {operation.Text}");
        }
    }

    public override IExpression VisitNumberFloor(Unipi.MppgParser.Grammar.MppgParser.NumberFloorContext context) =>
        Floor(context.numberExpression().Accept(this), context);

    public override IExpression VisitNumberCeil(Unipi.MppgParser.Grammar.MppgParser.NumberCeilContext context) =>
        Ceil(context.numberExpression().Accept(this), context);

    public override IExpression VisitEncNumberFloor(Unipi.MppgParser.Grammar.MppgParser.EncNumberFloorContext context) =>
        Floor(context.numberExpression().Accept(this), context);

    public override IExpression VisitEncNumberCeil(Unipi.MppgParser.Grammar.MppgParser.EncNumberCeilContext context) =>
        Ceil(context.numberExpression().Accept(this), context);

    private static IExpression Floor(IExpression argument, Antlr4.Runtime.ParserRuleContext context) =>
        argument is RationalExpression rE
            ? Expressions.Expressions.Floor(rE)
            : throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");

    private static IExpression Ceil(IExpression argument, Antlr4.Runtime.ParserRuleContext context) =>
        argument is RationalExpression rE
            ? Expressions.Expressions.Ceil(rE)
            : throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");

    public override IExpression VisitNumberAbs(Unipi.MppgParser.Grammar.MppgParser.NumberAbsContext context) =>
        Expressions.Expressions.AbsoluteValue(Operand(context.numberExpression(), context));

    public override IExpression VisitEncNumberAbs(Unipi.MppgParser.Grammar.MppgParser.EncNumberAbsContext context) =>
        Expressions.Expressions.AbsoluteValue(Operand(context.numberExpression(), context));

    public override IExpression VisitNumberPow(Unipi.MppgParser.Grammar.MppgParser.NumberPowContext context) =>
        Pow(context.numberExpression(), context);

    public override IExpression VisitEncNumberPow(Unipi.MppgParser.Grammar.MppgParser.EncNumberPowContext context) =>
        Pow(context.numberExpression(), context);

    public override IExpression VisitNumberMod(Unipi.MppgParser.Grammar.MppgParser.NumberModContext context) =>
        Expressions.Expressions.Remainder(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    public override IExpression VisitEncNumberMod(Unipi.MppgParser.Grammar.MppgParser.EncNumberModContext context) =>
        Expressions.Expressions.Remainder(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    public override IExpression VisitNumberGcd(Unipi.MppgParser.Grammar.MppgParser.NumberGcdContext context) =>
        Expressions.Expressions.GreatestCommonDivisor(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    public override IExpression VisitEncNumberGcd(Unipi.MppgParser.Grammar.MppgParser.EncNumberGcdContext context) =>
        Expressions.Expressions.GreatestCommonDivisor(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    public override IExpression VisitNumberLcm(Unipi.MppgParser.Grammar.MppgParser.NumberLcmContext context) =>
        Expressions.Expressions.LeastCommonMultiple(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    public override IExpression VisitEncNumberLcm(Unipi.MppgParser.Grammar.MppgParser.EncNumberLcmContext context) =>
        Expressions.Expressions.LeastCommonMultiple(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    /// <summary>
    /// The power of the two operands of <paramref name="operands"/>.
    /// The exponent is rejected unless it is an integer, which is the only kind the operation supports:
    /// it is truncated to one otherwise, which would silently give the power of a different exponent.
    /// </summary>
    private RationalExpression Pow(
        Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext[] operands,
        Antlr4.Runtime.ParserRuleContext context)
    {
        var @base = Operand(operands[0], context);
        var exponent = Operand(operands[1], context);

        var exponentValue = exponent.Compute();
        if (!exponentValue.IsInteger)
            throw new Exception(
                $"Invalid expression \"{context.GetJoinedText()}\": "
                + $"the exponent of pow must be an integer, but it is {exponentValue}");

        return Expressions.Expressions.Pow(@base, exponent);
    }

    private RationalExpression Operand(
        Unipi.MppgParser.Grammar.MppgParser.NumberExpressionContext operand,
        Antlr4.Runtime.ParserRuleContext context) =>
        operand.Accept(this) as RationalExpression
        ?? throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"");

    public override IExpression VisitNumberNegative(Unipi.MppgParser.Grammar.MppgParser.NumberNegativeContext context)
    {
        var ie = base.VisitNumberNegative(context);
        return ie switch
        {
            // shortcut for negated literals
            RationalNumberExpression rne => new RationalNumberExpression(-rne.Value),
            RationalExpression re => re.Negate(),
            _ => throw new Exception($"Invalid expression \"{context.GetJoinedText()}\"")
        };
    }
}
