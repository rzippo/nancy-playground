using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public partial class ExpressionVisitor : MppgBaseVisitor<IExpression>
{
    public State State { get; init; }

    public ExpressionVisitor(State? state)
    {
        State = state ?? new();
    }

    public override IExpression VisitExpression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context) =>
        context.GetChild(0).Accept(this);

    public override IExpression VisitFunctionEnclosedExpressionExp(
        Unipi.MppgParser.Grammar.MppgParser.FunctionEnclosedExpressionExpContext context) =>
        context.GetChild(0).Accept(this);

    public override IExpression VisitFunctionVariableExp(Unipi.MppgParser.Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new VariableNotFoundException($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    // The number tiers pass through to the tier below when they carry no operator of their own.
    public override IExpression VisitNumberSumAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberSumAtomContext context) =>
        context.numberProductExpression().Accept(this);

    public override IExpression VisitNumberUnaryAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberUnaryAtomContext context) =>
        context.numberEnclosedExpression().Accept(this);

    public override IExpression VisitEncNumberVariableExp(Unipi.MppgParser.Grammar.MppgParser.EncNumberVariableExpContext context)
    {
        var name = context.GetText();
        var (isPresent, type) = State.GetVariableType(name);
        if (!isPresent || type is null)
            throw new VariableNotFoundException($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitEncNumberLiteralExp(Unipi.MppgParser.Grammar.MppgParser.EncNumberLiteralExpContext context)
    {
        var numberLiteralContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext>(0);
        return ParseNumberLiteral(numberLiteralContext);
    }

    private static IExpression ParseNumberLiteral(
        Unipi.MppgParser.Grammar.MppgParser.NumberLiteralContext context)
    {
        var numberLiteralVisitor = new NumberLiteralVisitor();
        var value = numberLiteralVisitor.Visit(context);

        var valueExp = Expressions.Expressions.FromRational(value, "");
        return valueExp;
    }

}
