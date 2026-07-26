namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// The kind of output produced by a plot command, which determines the file extensions its <c>out</c> option accepts.
/// </summary>
public enum PlotOutputKind
{
    /// An image, as produced by <c>plot</c>.
    Image,

    /// TikZ code, as produced by <c>plotTikz</c>.
    Tikz
}

/// <summary>
/// Resolves the file name given to the <c>out</c> option of a plot command,
/// so that the user does not have to worry about the extension.
/// </summary>
public static class PlotOutPath
{
    private static readonly string[] ImageExtensions = [".png"];
    private static readonly string[] TikzExtensions = [".tex", ".tikz"];

    /// <summary>
    /// Returns <paramref name="outPath"/> with an extension that fits <paramref name="kind"/>.
    /// A compatible extension is left as is, a wrong one is replaced, a missing one is added:
    /// hence the result never has a double extension.
    /// </summary>
    public static string Resolve(string outPath, PlotOutputKind kind)
    {
        var (extensions, defaultExtension) = kind switch
        {
            PlotOutputKind.Tikz => (TikzExtensions, ".tex"),
            _ => (ImageExtensions, ".png")
        };

        var extension = Path.GetExtension(outPath);
        if (extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return outPath;

        // a wrong extension is replaced, while something that is not an extension (e.g., "rate-0.5") is preserved
        return IsExtensionLike(extension)
            ? Path.ChangeExtension(outPath, defaultExtension)
            : $"{outPath}{defaultExtension}";
    }

    /// <summary>
    /// True if the trailing part of the file name, as detected by <see cref="Path.GetExtension(string)"/>,
    /// looks like a file extension, rather than part of the name itself.
    /// </summary>
    private static bool IsExtensionLike(string extension)
    {
        return extension.Length is > 1 and <= 6
            && extension.Skip(1).All(char.IsLetter);
    }
}
