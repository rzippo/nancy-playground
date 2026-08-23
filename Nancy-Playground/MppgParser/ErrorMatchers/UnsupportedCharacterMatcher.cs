namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A character the lexer cannot read, with nothing on the line to say what it was meant to be.
/// </summary>
/// <remarks>
/// The other way round from <see cref="CharacterInANameMatcher"/> on <see cref="LexerError.LooksLikeANameBeingWritten"/>, so that the two cannot both recognise the same error.
/// </remarks>
internal sealed class UnsupportedCharacterMatcher : IErrorMatcher<LexerError>
{
    /// <inheritdoc/>
    public string Name => "character not supported";

    /// <inheritdoc/>
    public bool Recognises(LexerError error)
        => error.IsACharacterOfItsOwn && !error.LooksLikeANameBeingWritten;

    /// <inheritdoc/>
    public RewrittenMessage Write(LexerError error)
        => new($"'{error.Character}' is not a supported character");
}
