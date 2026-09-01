namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// An operation of a version later than the one the program is read with, which the older syntax has no keyword for and so reads as a name.
/// </summary>
/// <remarks>
/// The other side of the hint of <see cref="VersionedKeywords"/>, which is for a program of an older version naming a variable after a keyword the newer one has.
/// </remarks>
internal sealed class KeywordOfALaterVersionMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "operation of a later syntax version";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => error.KeywordOfALaterVersion is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
    {
        var (token, keyword, introducedIn) = error.KeywordOfALaterVersion!.Value;

        return new(
            $"'{keyword}' is not an operation of syntax version {error.Version}",
            $"'{keyword}' is an operation from version {introducedIn} on: to use it, declare '#!syntax version {introducedIn}', or later, before any other statement.",
            Position: token);
    }
}
