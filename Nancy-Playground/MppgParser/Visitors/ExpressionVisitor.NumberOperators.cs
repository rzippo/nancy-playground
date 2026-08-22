using Unipi.Nancy.Expressions;
using Unipi.Nancy.Expressions.Internals;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor
{
    /// <summary>
    /// Builds the expression of a product with no operator, i.e. the operand alone.
    /// </summary>
    public override IExpression VisitNumberProductAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberProductAtomContext context) =>
        context.numberUnaryExpression().Accept(this);

    /// <summary>
    /// Builds a multiplication or a division between numbers.
    /// </summary>
    public override IExpression VisitNumberProductMulDiv(
        Unipi.MppgParser.Grammar.MppgParser.NumberProductMulDivContext context)
    {
        var left = (RationalExpression)context.numberProductExpression().Accept(this);
        var right = (RationalExpression)context.numberUnaryExpression().Accept(this);

        return ApplyNumberMulDiv(left, context.op.Type, right);
    }

    private static RationalExpression ApplyNumberMulDiv(
        RationalExpression left,
        int operationType,
        RationalExpression right) =>
        operationType switch
        {
            Unipi.MppgParser.Grammar.MppgParser.PROD_SIGN => RationalExpression.Product(left, right),
            Unipi.MppgParser.Grammar.MppgParser.DIV_SIGN => RationalExpression.Division(left, right),
            Unipi.MppgParser.Grammar.MppgParser.DIV_OP => RationalExpression.Division(left, right),
            Unipi.MppgParser.Grammar.MppgParser.MOD_OP => Expressions.Expressions.Remainder(left, right),
            _ => throw new InvalidOperationException($"Unexpected operation type: {operationType}")
        };

    /// <summary>
    /// Builds a sum, a subtraction, a minimum or a maximum between numbers.
    /// </summary>
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

    /// <summary>
    /// Builds the expression of 'floor' applied to a number.
    /// </summary>
    public override IExpression VisitEncNumberFloor(Unipi.MppgParser.Grammar.MppgParser.EncNumberFloorContext context) =>
        Floor(context.numberExpression().Accept(this), context);

    /// <summary>
    /// Builds the expression of 'ceil' applied to a number.
    /// </summary>
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

    /// <summary>
    /// Builds the expression of 'abs', the absolute value of a number.
    /// </summary>
    public override IExpression VisitEncNumberAbs(Unipi.MppgParser.Grammar.MppgParser.EncNumberAbsContext context) =>
        Expressions.Expressions.AbsoluteValue(Operand(context.numberExpression(), context));

    /// <summary>
    /// Builds the expression of 'pow', a number raised to a power.
    /// </summary>
    public override IExpression VisitEncNumberPow(Unipi.MppgParser.Grammar.MppgParser.EncNumberPowContext context) =>
        Pow(context.numberExpression(), context);

    /// <summary>
    /// Builds the expression of 'gcd', the greatest common divisor of two numbers.
    /// </summary>
    public override IExpression VisitEncNumberGcd(Unipi.MppgParser.Grammar.MppgParser.EncNumberGcdContext context) =>
        Expressions.Expressions.GreatestCommonDivisor(
            Operand(context.numberExpression(0), context),
            Operand(context.numberExpression(1), context));

    /// <summary>
    /// Builds the expression of 'lcm', the least common multiple of two numbers.
    /// </summary>
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

    /// <summary>
    /// Builds the negation of a number, as in -x.
    /// </summary>
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
