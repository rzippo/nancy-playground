namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// A version of the MPPG syntax, which decides the keywords a program is read with.
/// </summary>
public readonly record struct SyntaxVersion(int Major, int Minor) : IComparable<SyntaxVersion>
{
    /// <summary>
    /// The syntax as the original console defines it.
    /// </summary>
    public static readonly SyntaxVersion V1_0 = new(1, 0);
    /// <summary>
    /// Adds the operators of version 1.1.
    /// </summary>
    public static readonly SyntaxVersion V1_1 = new(1, 1);
    /// <summary>
    /// Adds the operators of version 1.2.
    /// </summary>
    public static readonly SyntaxVersion V1_2 = new(1, 2);
    /// <summary>
    /// Adds the operators of version 1.3.
    /// </summary>
    public static readonly SyntaxVersion V1_3 = new(1, 3);
    /// <summary>
    /// Adds the non-increasing closures and property assertions of version 1.4.
    /// </summary>
    public static readonly SyntaxVersion V1_4 = new(1, 4);
    /// <summary>
    /// The most recent version, which a program is read with unless it declares another.
    /// </summary>
    public static readonly SyntaxVersion Latest = V1_4;

    /// Every version of the syntax, in order.
    public static readonly IReadOnlyList<SyntaxVersion> All = [V1_0, V1_1, V1_2, V1_3, V1_4];

    /// The version that precedes this one, or null if this is the first.
    public SyntaxVersion? Previous()
    {
        SyntaxVersion? previous = null;
        foreach (var version in All)
        {
            if (version >= this)
                break;
            previous = version;
        }
        return previous;
    }

    /// <summary>
    /// The version made of the given parts.
    /// </summary>
    public static SyntaxVersion FromParts(int major, int minor) => new(major, minor);

    /// <summary>
    /// Reads the version a <c>#!syntax version X.Y</c> line declares, returning false where the line does not declare one.
    /// </summary>
    public static bool TryParseShebang(string shebang, out SyntaxVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(shebang) || !shebang.StartsWith("#!syntax"))
            return false;

        var parts = shebang.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !parts[2].Contains('.'))
            return false;

        var versionParts = parts[2].Split('.');
        if (versionParts.Length != 2
            || !int.TryParse(versionParts[0], out var major)
            || !int.TryParse(versionParts[1], out var minor))
            return false;

        version = new SyntaxVersion(major, minor);
        return true;
    }

    /// <summary>
    /// Orders by major version first, then by minor.
    /// </summary>
    public int CompareTo(SyntaxVersion other)
        => Major != other.Major
            ? Major.CompareTo(other.Major)
            : Minor.CompareTo(other.Minor);

    /// <summary>
    /// True where <paramref name="a"/> is the same version as <paramref name="b"/> or a later one.
    /// </summary>
    public static bool operator >=(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) >= 0;
    /// <summary>
    /// True where <paramref name="a"/> is the same version as <paramref name="b"/> or an earlier one.
    /// </summary>
    public static bool operator <=(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) <= 0;
    /// <summary>
    /// True where <paramref name="a"/> is a later version than <paramref name="b"/>.
    /// </summary>
    public static bool operator >(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) > 0;
    /// <summary>
    /// True where <paramref name="a"/> is an earlier version than <paramref name="b"/>.
    /// </summary>
    public static bool operator <(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) < 0;

    /// <summary>
    /// The version as it is written in a directive, i.e. <c>major.minor</c>.
    /// </summary>
    public override string ToString() => $"{Major}.{Minor}";
}
