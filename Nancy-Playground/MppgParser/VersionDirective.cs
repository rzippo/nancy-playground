namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// Reads the version a '#!syntax version' directive declares, and says why it cannot be used when it
/// declares one this build does not have.
/// </summary>
public static class VersionDirective
{
    /// <summary>
    /// The version declared by <paramref name="text"/>, or null if it is not one that can be applied.
    /// </summary>
    /// <param name="text">The line to read, which is a '#!syntax version' directive.</param>
    /// <param name="error">Why the directive cannot be applied, or null if it can.</param>
    public static SyntaxVersion? Read(string text, out string? error)
    {
        if (!SyntaxVersion.TryParseShebang(text, out var version))
        {
            error = $"'{text.Trim()}' is not a version directive, which is written "
                + $"'#!syntax version <major>.<minor>', e.g. '#!syntax version {SyntaxVersion.Latest}'";
            return null;
        }

        if (version > SyntaxVersion.Latest)
        {
            error = $"syntax version {version} is not supported by this build, the latest being {SyntaxVersion.Latest}";
            return null;
        }

        if (!SyntaxVersion.All.Contains(version))
        {
            error = $"{version} is not a known version of the syntax, the known ones being "
                + $"{string.Join(", ", SyntaxVersion.All)}";
            return null;
        }

        error = null;
        return version;
    }

    /// <summary>
    /// What to add to the error of a version later than <see cref="SyntaxVersion.Latest"/>, which the
    /// grammar gates as the latest one: the script parsed as the latest, so the errors it has, or does
    /// not have, tell whether it needs the version it asks for.
    /// </summary>
    public static string TooRecentHint(SyntaxVersion declared, bool hasOtherErrors)
        => hasOtherErrors
            ? $"Some of the errors below may be constructs of {declared}, which this build does not have."
            : $"The script parsed with the constructs of {SyntaxVersion.Latest}, so it can declare "
                + $"'#!syntax version {SyntaxVersion.Latest}'.";
}
