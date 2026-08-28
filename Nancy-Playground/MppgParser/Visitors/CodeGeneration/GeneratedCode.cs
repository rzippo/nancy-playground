using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

internal sealed class GeneratedCode
{
    private readonly ExpressionSyntax? _expression;
    private readonly IReadOnlyList<GeneratedCodeEntry> _entries;

    private GeneratedCode(ExpressionSyntax? expression, IReadOnlyList<GeneratedCodeEntry> entries)
    {
        _expression = expression;
        _entries = entries;
    }

    public static GeneratedCode Empty { get; } = new(null, []);

    public static GeneratedCode Expression(ExpressionSyntax expression) =>
        new(expression, []);

    public static GeneratedCode Entries(IEnumerable<GeneratedCodeEntry> entries) =>
        new(null, entries.ToList());

    public ExpressionSyntax SingleExpression() =>
        _expression ?? throw new InvalidOperationException("Expected a generated C# expression.");

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
