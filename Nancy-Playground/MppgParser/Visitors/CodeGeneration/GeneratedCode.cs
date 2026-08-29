using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

internal sealed class GeneratedCode
{
    private readonly ExpressionSyntax? _expression;
    private readonly IReadOnlyList<GeneratedCodeEntry> _entries;

    private GeneratedCode(ExpressionSyntax? expression, IReadOnlyList<GeneratedCodeEntry> entries, bool isBareIntLiteral)
    {
        _expression = expression;
        _entries = entries;
        IsBareIntLiteral = isBareIntLiteral;
    }

    public static GeneratedCode Empty { get; } = new(null, [], false);

    public static GeneratedCode Expression(ExpressionSyntax expression) =>
        new(expression, [], false);

    /// <summary>
    /// A number expression built (in the direct Nancy API profile only) from bare int arithmetic,
    /// with no Rational anywhere in it yet: safe wherever the position implicitly converts it (an
    /// argument, an assignment target, an operand of +, -, or *), unsafe as an operand of / against
    /// another bare one, where it would resolve to C#'s int division instead of Rational's.
    /// </summary>
    public static GeneratedCode Expression(ExpressionSyntax expression, bool isBareIntLiteral) =>
        new(expression, [], isBareIntLiteral);

    public static GeneratedCode Entries(IEnumerable<GeneratedCodeEntry> entries) =>
        new(null, entries.ToList(), false);

    public ExpressionSyntax SingleExpression() =>
        _expression ?? throw new InvalidOperationException("Expected a generated C# expression.");

    public bool IsBareIntLiteral { get; }

    public IReadOnlyList<GeneratedCodeEntry> EntriesOrEmpty() => _entries;
}

/// <summary>
/// Thrown when a visitor reaches a grammar rule it has no code generation for.
/// Caught at the statement level, where it is reported as a NOT IMPLEMENTED comment.
/// </summary>
internal sealed class NotImplementedCodeGenerationException : Exception
{
    public NotImplementedCodeGenerationException(string ruleText)
        : base($"No code generation is implemented for: {ruleText}")
    {
    }
}

internal abstract record GeneratedCodeEntry;

internal sealed record GeneratedStatementEntry(StatementSyntax Statement) : GeneratedCodeEntry;

internal sealed record GeneratedCommentEntry(string Text) : GeneratedCodeEntry;

internal sealed record GeneratedBlankLineEntry : GeneratedCodeEntry
{
    public static GeneratedBlankLineEntry Instance { get; } = new();
}
