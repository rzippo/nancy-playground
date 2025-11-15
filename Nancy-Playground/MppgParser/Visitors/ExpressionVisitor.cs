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
            throw new VariableNotFoundException($"Variable '{name}' not found");
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
            throw new VariableNotFoundException($"Variable '{name}' not found");
        if (type == ExpressionType.Function)
            return State.GetFunctionVariable(name);
        else
            return State.GetNumberVariable(name);
    }

    public override IExpression VisitNumberLiteralExp(Grammar.MppgParser.NumberLiteralExpContext context)
    {
        var numberLiteralVisitor = new NumberLiteralVisitor();
        var value = numberLiteralVisitor.Visit(context);
        
        var valueExp = Expressions.FromRational(value, "");
        return valueExp;
    }
    
    public override IExpression VisitEncNumberLiteralExp(Grammar.MppgParser.EncNumberLiteralExpContext context)
    {
        var numberLiteralVisitor = new NumberLiteralVisitor();
        var value = numberLiteralVisitor.Visit(context);
        
        var valueExp = Expressions.FromRational(value, "");
        return valueExp;
    }
}