using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A keyword written where a name belongs, which is what a script written before the keyword existed does, and what a name chosen from the vocabulary of the domain does.
/// </summary>
/// <remarks>
/// Recognised by the assignment that follows the keyword, which is what says the user meant to name it: the error is reported at the keyword in <c>div := 3</c> and at the <c>:=</c> in <c>star := 3</c>, so whichever of the two the error did not land on is read.
/// A keyword elsewhere is not claimed, a keyword being wrong in a place where it is also the operator it spells: in <c>( floor comp (C / 2) )</c> the error is reported at <c>comp</c>, which is used correctly, the mistake being the <c>floor</c> before it.
/// The hint of <see cref="VersionedKeywords"/> stays what tells the versioned ones apart, saying which version introduced the keyword and what to declare to keep the name.
/// </remarks>
internal sealed class KeywordUsedAsNameMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "keyword used as a name";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => KeywordBeingNamed(error) is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"'{KeywordBeingNamed(error)!.Text}' is a keyword, so it cannot be a name");

    /// <summary>
    /// The keyword being named, read from whichever side of the assignment the error did not land on, or null where no name was being written.
    /// </summary>
    private static IToken? KeywordBeingNamed(ParserError error)
    {
        // the keyword is the offending token, and the assignment follows it
        if (TokenFacts.IsKeywordSpelledLikeAName(error.Tokens.Offending)
            && error.Tokens.Next?.Type == Unipi.MppgParser.Grammar.MppgLexer.ASSIGN)
            return error.Tokens.Offending;

        // the parser read past the keyword, and stopped at the assignment that follows it
        if (error.Tokens.Offending?.Type == Unipi.MppgParser.Grammar.MppgLexer.ASSIGN
            && TokenFacts.IsKeywordSpelledLikeAName(error.Tokens.Previous))
            return error.Tokens.Previous;

        return null;
    }
}
