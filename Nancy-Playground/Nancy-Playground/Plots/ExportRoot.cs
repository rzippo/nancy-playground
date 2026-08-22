namespace Unipi.Nancy.Playground.Cli.Plots;

/// <summary>
/// The directory the files written by a session are saved in, i.e. plots for <c>run</c>,
/// plots and exported programs for <c>interactive</c>.
/// </summary>
public sealed record ExportRoot
{
    /// <summary>
    /// The absolute path of the directory.
    /// </summary>
    public string Path { get; }

    private ExportRoot(string path)
    {
        Path = path;
    }

    /// <summary>
    /// The root of a <c>run</c>, from the plot options of the command.
    /// </summary>
    /// <param name="plotsRoot">The explicit directory, if given.</param>
    /// <param name="plotsRootMode">The mode to derive the directory from, if given.</param>
    /// <param name="scriptDirectory">The directory of the script being run.</param>
    /// <exception cref="InvalidOperationException">If the two options contradict each other.</exception>
    public static ExportRoot ForRun(string? plotsRoot, PlotRootMode? plotsRootMode, string? scriptDirectory)
    {
        if (!string.IsNullOrWhiteSpace(plotsRoot))
        {
            if (plotsRootMode.HasValue && plotsRootMode.Value != PlotRootMode.Custom)
                throw new InvalidOperationException("--plots-root is specified with an explicit path, so --plots-root-mode must be Custom or omitted.");

            return new ExportRoot(System.IO.Path.GetFullPath(plotsRoot));
        }

        var mode = plotsRootMode ?? PlotRootMode.ScriptDirectory;
        var path = mode switch
        {
            PlotRootMode.ScriptDirectory => scriptDirectory,
            PlotRootMode.CurrentDirectory => Directory.GetCurrentDirectory(),
            PlotRootMode.Custom => throw new InvalidOperationException("--plots-root-mode is Custom but --plots-root was not specified."),
            _ => scriptDirectory,
        };

        return new ExportRoot(System.IO.Path.GetFullPath(path ?? Directory.GetCurrentDirectory()));
    }

    /// <summary>
    /// The root of an <c>interactive</c> session, which has no script to derive a directory from.
    /// </summary>
    /// <param name="exportRoot">The explicit directory, if given.</param>
    public static ExportRoot ForInteractive(string? exportRoot)
    {
        return new ExportRoot(System.IO.Path.GetFullPath(
            string.IsNullOrWhiteSpace(exportRoot) ? Directory.GetCurrentDirectory() : exportRoot
        ));
    }

    /// <summary>
    /// The absolute path <paramref name="userPath"/> refers to, resolved against this root if relative.
    /// </summary>
    public string Resolve(string userPath) =>
        System.IO.Path.GetFullPath(userPath, Path);

    /// <summary>
    /// Checks that the directory exists, so that a run fails before doing any work rather than at
    /// the first write.
    /// </summary>
    /// <returns>The error to report, or null if the directory can be written to.</returns>
    public string? Validate() =>
        Directory.Exists(Path) ? null : $"{Path}: directory not found.";

    /// <summary>
    /// The directory as a path.
    /// </summary>
    public override string ToString() => Path;
}
