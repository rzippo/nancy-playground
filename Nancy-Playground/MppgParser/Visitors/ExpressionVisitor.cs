using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Exceptions;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Builds the Nancy expression a parse tree describes, resolving names against the session it is given.
/// </summary>
public partial class ExpressionVisitor : MppgBaseVisitor<IExpression>
{
    /// <summary>
    /// The variables the expression is resolved against.
    /// </summary>
    public State State { get; init; }

    /// <summary>
    /// A visitor resolving names against <paramref name="state"/>, or against nothing where it is null.
    /// </summary>
    public ExpressionVisitor(State? state)
    {
        State = state ?? new();
    }

    /// <summary>
    /// Builds the expression of a whole expression, whichever kind it resolves to.
    /// </summary>
    public override IExpression VisitExpression(Unipi.MppgParser.Grammar.MppgParser.ExpressionContext context) =>
        context.GetChild(0).Accept(this);

    /// <summary>
    /// Builds the expression of a bracketed function expression.
    /// </summary>
    public override IExpression VisitFunctionEnclosedExpressionExp(
        Unipi.MppgParser.Grammar.MppgParser.FunctionEnclosedExpressionExpContext context) =>
        context.GetChild(0).Accept(this);

    /// <summary>
    /// Builds the expression a function variable stands for, i.e. what the name was assigned.
    /// </summary>
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
    /// <summary>
    /// Builds the expression of a product where no sum follows it.
    /// </summary>
    public override IExpression VisitNumberSumAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberSumAtomContext context) =>
        context.numberProductExpression().Accept(this);

    /// <summary>
    /// Builds the expression of a bracketed number expression.
    /// </summary>
    public override IExpression VisitNumberUnaryAtom(
        Unipi.MppgParser.Grammar.MppgParser.NumberUnaryAtomContext context) =>
        context.numberEnclosedExpression().Accept(this);

    /// <summary>
    /// Builds the expression a number variable stands for, i.e. what the name was assigned.
    /// </summary>
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

    /// <summary>
    /// Builds the expression of a number literal.
    /// </summary>
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
