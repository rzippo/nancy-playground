namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A character the lexer cannot read, where the line says it was being written as a name.
/// </summary>
/// <remarks>
/// The message says what was being written and the hint says what the lexer found, so that the reader is pointed at the name they meant rather than at the character alone.
/// </remarks>
internal sealed class CharacterInANameMatcher : IErrorMatcher<LexerError>
{
    /// <inheritdoc/>
    public string Name => "character written in a name";

    /// <inheritdoc/>
    public bool Recognises(LexerError error)
        => error.IsACharacterOfItsOwn && error.LooksLikeANameBeingWritten;

    /// <inheritdoc/>
    public RewrittenMessage Write(LexerError error)
        => new($"'{error.Character}' is not a valid variable name",
            $"'{error.Character}' is not a supported character.");
}
