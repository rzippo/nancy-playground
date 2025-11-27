namespace Unipi.MppgParser.Utility;

public static class StringExtensions
{
    public static string TrimQuotes(this string str)
    {
        if(str.StartsWith('"'))
            str = str[1..];
        if (str.EndsWith('"'))
            str = str[0..^1];
        return str;
    }
    
    /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <inheritdoc cref="string.IsNullOrEmpty"/>
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
}