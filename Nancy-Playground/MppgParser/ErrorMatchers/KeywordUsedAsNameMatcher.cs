namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A keyword written where a name belongs, which is what a script written before the keyword existed does, and what a name chosen from the vocabulary of the domain does.
/// </summary>
/// <remarks>
/// A keyword elsewhere is not claimed, a keyword being wrong in a place where it is also the operator it spells: in <c>( floor comp (C / 2) )</c> the error is reported at <c>comp</c>, which is used correctly, the mistake being the <c>floor</c> before it.
/// The hint of <see cref="VersionedKeywords"/> stays what tells the versioned ones apart, saying which version introduced the keyword and what to declare to keep the name.
/// </remarks>
internal sealed class KeywordUsedAsNameMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "keyword used as a name";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => error.KeywordBeingNamed is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"'{error.KeywordBeingNamed!.Text}' is a keyword, so it cannot be a name");
}
