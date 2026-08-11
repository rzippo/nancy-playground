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
