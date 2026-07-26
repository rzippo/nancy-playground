namespace Unipi.Nancy.Playground.MppgParser;

public readonly record struct SyntaxVersion(int Major, int Minor) : IComparable<SyntaxVersion>
{
    public static readonly SyntaxVersion V1_0 = new(1, 0);
    public static readonly SyntaxVersion V1_1 = new(1, 1);
    public static readonly SyntaxVersion Latest = V1_1;

    public static SyntaxVersion FromParts(int major, int minor) => new(major, minor);

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

    public int CompareTo(SyntaxVersion other)
        => Major != other.Major
            ? Major.CompareTo(other.Major)
            : Minor.CompareTo(other.Minor);

    public static bool operator >=(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) >= 0;
    public static bool operator <=(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) > 0;
    public static bool operator <(SyntaxVersion a, SyntaxVersion b) => a.CompareTo(b) < 0;

    public override string ToString() => $"{Major}.{Minor}";
}
