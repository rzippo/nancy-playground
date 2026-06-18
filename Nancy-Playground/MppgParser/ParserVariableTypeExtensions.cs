using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser;

internal static class ParserVariableTypeExtensions
{
    public static void SeedVariableTypes(
        this Unipi.MppgParser.Grammar.MppgParser parser,
        State? state
    )
    {
        if (state is null)
            return;

        foreach (var pair in state.GetVariableTypes())
        {
            parser.SetVariableType(pair.Key, pair.Value switch
            {
                ExpressionType.Function => Unipi.MppgParser.Grammar.MppgParser.VariableType.Function,
                ExpressionType.Number => Unipi.MppgParser.Grammar.MppgParser.VariableType.Number,
                _ => throw new InvalidOperationException($"Cannot seed variable '{pair.Key}' with undetermined type.")
            });
        }
    }
}
