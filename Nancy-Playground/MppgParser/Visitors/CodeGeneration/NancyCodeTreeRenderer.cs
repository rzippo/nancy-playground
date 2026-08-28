using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

internal static class NancyCodeTreeRenderer
{
    public static List<string> RenderLines(CompilationUnitSyntax compilationUnit)
    {
        var source = compilationUnit
            .NormalizeWhitespace(eol: "\n")
            .ToFullString()
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        return source
            .Split('\n')
            .Select(static line => line.TrimEnd('\r'))
            .ToList();
    }
}
