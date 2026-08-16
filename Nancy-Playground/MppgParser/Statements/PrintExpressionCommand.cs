using Unipi.Nancy.Expressions;
using Unipi.Nancy.Playground.MppgParser.Statements.Formatters;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

public record class PrintExpressionCommand : Statement
{
    public string VariableName { get; set; }

    public PrintExpressionCommand(string variableName)
    {
        VariableName = variableName;
    }

    public override string Execute(State state)
    {
        var (exists, type) = state.GetVariableType(VariableName);
        if(!exists)
            return $"ERROR: Variable \"{VariableName}\" not found";
        else
        {
            switch (type)
            {
                case ExpressionType.Function:
                case ExpressionType.Number:
                {
                    return ExecuteToFormattable(state).OutputText;
                }
                default:
                {
                    return $"ERROR: Unknown expression type for variable \"{VariableName}\"";
                }
            }
        }
    }

    public override StatementOutput ExecuteToFormattable(State state)
    {
        var (exists, type) = state.GetVariableType(VariableName);
        if(!exists)
            throw new Exception($"Variable \"{VariableName}\" not found");
        else
        {
            IExpression expression;
            switch (type)
            {
                case ExpressionType.Function:
                {
                    expression = state.GetFunctionVariable(VariableName);
                    break;
                }
                case ExpressionType.Number:
                {
                    expression = state.GetNumberVariable(VariableName);
                    break;
                }
                default:
                {
                    throw new Exception($"Unknown expression type for variable \"{VariableName}\"");
                }
            }

            return new PrintExpressionOutput
            {
                StatementText = Text,
                // the default notation is the syntax itself, and the notation of Nancy is what stands
                // in when the syntax cannot write an operation
                OutputText = MppgOutput.OfExpression(expression, NancyOutput.OfExpression(expression)),
                Expression = expression
            };
        }
    }
}