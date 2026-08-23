using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// Where an error is, and the source around it.
/// </summary>
/// <param name="Line">The line it is on, counted from one.</param>
/// <param name="Column">The column it is at, counted from zero.</param>
/// <param name="SourceLine">That line, raw and unescaped, or null where the source is not available.</param>
/// <param name="PreviousLine">The line before it, for a display layer to show as context.</param>
internal sealed record SourcePosition(int Line, int Column, string? SourceLine, string? PreviousLine);

/// <summary>
/// The tokens the parser would have accepted where it stopped.
/// </summary>
/// <param name="Names">How the vocabulary spells them, quotes and all, which is what reads in a message.</param>
/// <param name="Types">The same tokens by type, which compare without depending on that spelling.</param>
internal sealed record ExpectedTokens(IReadOnlyList<string> Names, IReadOnlyList<int> Types)
{
    /// <summary>
    /// Nothing was expected, which is the case for an error no parser reported.
    /// </summary>
    public static readonly ExpectedTokens None = new([], []);

    /// <summary>
    /// How many tokens would have fitted.
    /// </summary>
    public int Count => Types.Count;

    /// <summary>
    /// The one token that would have fitted, spelled without the quotes the vocabulary puts around it, or null where more than one would.
    /// </summary>
    public string? Only => Names.Count == 1 ? Names[0].Trim('\'') : null;

    /// <summary>
    /// True where nothing but the end of the statement would have fitted, i.e. the statement was read whole and something follows it.
    /// </summary>
    /// <remarks>
    /// A program expects the end of a line or of the input, a single line only the end of the input, its entry rule being anchored there: both mean that the statement was read whole.
    /// </remarks>
    public bool AreOnlyTheEndOfTheStatement
        => Types.Count > 0
            && Types.Contains(TokenConstants.EOF)
            && Types.All(type => type == TokenConstants.EOF
                || type == Unipi.MppgParser.Grammar.MppgLexer.NEW_LINE);
}

/// <summary>
/// What the parser did to carry on past the error, where it did anything.
/// </summary>
/// <remarks>
/// The two recoveries are reported the same way, with no exception and through the same call, so the parser is asked which one it made rather than its message read for the answer.
/// </remarks>
internal enum ParserRecovery
{
    /// <summary>
    /// Nothing: the error was raised rather than recovered from.
    /// </summary>
    None,

    /// <summary>
    /// A token was invented, which is a bracket left open or a separator left out.
    /// </summary>
    MissingToken,

    /// <summary>
    /// A token was dropped, which is one too many.
    /// </summary>
    UnwantedToken
}

/// <summary>
/// The token the parser could not use, and the two beside it.
/// </summary>
/// <remarks>
/// The neighbours say what was being written where the offending token alone does not: a keyword used as a name is reported at the keyword in 'div := 3' and at the ':=' in 'star := 3', so both are needed to recognise the one mistake.
/// </remarks>
/// <param name="Offending">The token that could not be used.</param>
/// <param name="Previous">The token before it, or null where it opens the input.</param>
/// <param name="Next">The token after it, or null where it ends the input.</param>
internal sealed record ErrorTokens(IToken? Offending, IToken? Previous, IToken? Next);

/// <summary>
/// The rule being parsed when the error was found.
/// </summary>
/// <param name="Name">Its name, innermost first in <paramref name="Stack"/>.</param>
/// <param name="Stack">The rules it was nested in, innermost first.</param>
/// <param name="StartToken">
/// Its first token, which is the name of the construct being written where the rule names of the grammar are internal: 'bucket' where the rule is called tokenBucket.
/// </param>
internal sealed record ParsedRule(string? Name, IReadOnlyList<string> Stack, IToken? StartToken)
{
    /// <summary>
    /// No rule, which is the case for an error no parser reported.
    /// </summary>
    public static readonly ParsedRule None = new(null, [], null);

    /// <summary>
    /// True where the error was found inside <paramref name="ruleName"/>, i.e. it is on the stack.
    /// </summary>
    public bool IsInside(string ruleName) => Stack.Contains(ruleName);
}

/// <summary>
/// One error found while parsing, of whichever kind: what is known of it, before anything is written about it.
/// </summary>
/// <remarks>
/// Each kind holds what that kind knows and nothing else, so a reader of one cannot ask for a field that its kind never has.
/// The message layer turns these into what a user is shown.
/// </remarks>
/// <param name="Position">Where the error is.</param>
/// <param name="DefaultMessage">What to show when no pattern recognises the error.</param>
/// <param name="DefaultHint">What to add to it, where something is known beyond the message.</param>
internal abstract record ParseError(SourcePosition Position, string DefaultMessage, string? DefaultHint)
{
    /// <summary>
    /// What ANTLR said, or null where the playground raised the error itself.
    /// </summary>
    public abstract string? AntlrMessage { get; }

    /// <summary>
    /// The text the error is about, which is what a message quotes back.
    /// </summary>
    public abstract string? OffendingText { get; }
}

/// <summary>
/// A character no token rule accepts, which the lexer stops at.
/// </summary>
/// <param name="Position">Where the character is.</param>
/// <param name="Character">The character itself, read from the input.</param>
/// <param name="LexerMessage">What ANTLR said about it.</param>
internal sealed record LexerError(SourcePosition Position, string? Character, string LexerMessage)
    : ParseError(Position, LexerMessage, null)
{
    /// <inheritdoc/>
    public override string? AntlrMessage => LexerMessage;

    /// <inheritdoc/>
    public override string? OffendingText => Character;

    /// <summary>
    /// True where the error is about a character in its own right, rather than about the quote that opens a string the lexer never finds the end of.
    /// </summary>
    public bool IsACharacterOfItsOwn => Character is { Length: > 0 } and not ['"', ..];

    /// <summary>
    /// True where the character stands before the assignment of its line, which is what says a name was being written: it holds wherever in the name the character is, as in <c>@ab</c>, <c>ab@</c> and <c>a@b</c> alike.
    /// </summary>
    /// <remarks>
    /// Read from the line rather than from the tokens, there being none: the lexer stopped before it could make any.
    /// </remarks>
    public bool LooksLikeANameBeingWritten
    {
        get
        {
            if (Position.SourceLine is not { } line || Position.Column < 0 || Position.Column >= line.Length)
                return false;

            var assignment = line.IndexOf(":=", StringComparison.Ordinal);
            return assignment >= 0 && Position.Column < assignment;
        }
    }
}

/// <summary>
/// A token the parser could not use where it stood.
/// </summary>
/// <param name="Position">Where the token is.</param>
/// <param name="Tokens">The token, and the two beside it.</param>
/// <param name="Rule">The rule being parsed when it was met.</param>
/// <param name="Expected">What would have fitted instead.</param>
/// <param name="Exception">
/// The exception the error came with, or null when it comes from the recovery of the parser, which is how a missing or an extraneous token is reported.
/// </param>
/// <param name="DeclaredVariables">The variables declared up to the error, which a name has to be among to be one.</param>
/// <param name="ParserMessage">What ANTLR said about it.</param>
/// <param name="Recovery">What the parser did to carry on, which is what tells a token it invented from one it dropped.</param>
/// <param name="DefaultHint">What to add to the message, where something is known beyond it.</param>
internal sealed record ParserError(
    SourcePosition Position,
    ErrorTokens Tokens,
    ParsedRule Rule,
    ExpectedTokens Expected,
    RecognitionException? Exception,
    IReadOnlyDictionary<string, Unipi.MppgParser.Grammar.MppgParser.VariableType> DeclaredVariables,
    string ParserMessage,
    string? DefaultHint = null,
    ParserRecovery Recovery = ParserRecovery.None
) : ParseError(Position, ParserMessage, DefaultHint)
{
    /// <inheritdoc/>
    public override string? AntlrMessage => ParserMessage;

    /// <inheritdoc/>
    public override string? OffendingText => Tokens.Offending?.Text;

    /// <summary>
    /// No variable declared, which is the case where the parser cannot be asked for them.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Unipi.MppgParser.Grammar.MppgParser.VariableType> NoVariables
        = new Dictionary<string, Unipi.MppgParser.Grammar.MppgParser.VariableType>();

    /// <summary>
    /// True if <paramref name="name"/> is one of the variables declared up to the error.
    /// </summary>
    public bool IsDeclared(string name) => DeclaredVariables.ContainsKey(name);
}

/// <summary>
/// A '#!syntax version' directive this build cannot apply, which the playground reports itself rather than the parser, the directive being well formed as far as the grammar is concerned.
/// </summary>
/// <param name="Position">Where the directive is.</param>
/// <param name="DirectiveText">The directive as it is written.</param>
/// <param name="DeclaredVersion">The version it declares, or null where it does not declare one.</param>
/// <param name="Reason">Why it cannot be applied, which is the message shown.</param>
/// <param name="DefaultHint">What to do about it, where there is something to suggest.</param>
internal sealed record UnusableVersionDirectiveError(
    SourcePosition Position,
    string DirectiveText,
    SyntaxVersion? DeclaredVersion,
    string Reason,
    string? DefaultHint = null
) : ParseError(Position, Reason, DefaultHint)
{
    /// <inheritdoc/>
    public override string? AntlrMessage => null;

    /// <inheritdoc/>
    public override string? OffendingText => DirectiveText;
}

/// <summary>
/// What a token spells, asked of it without reading the grammar.
/// </summary>
internal static class TokenFacts
{
    /// <summary>
    /// True if <paramref name="token"/> is a keyword spelled the way a name is, i.e. one that a user can mean as a variable: 'div' and 'star' are, ':=' and '(' are not.
    /// </summary>
    public static bool IsKeywordSpelledLikeAName(IToken? token)
        => token is not null
            && token.Type != Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            && token.Text.Length > 0
            && (char.IsLetter(token.Text[0]) || token.Text[0] == '_')
            && token.Text.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

    /// <summary>
    /// True if <paramref name="token"/> is where the line, or the input, ends.
    /// </summary>
    public static bool EndsTheLine(IToken? token)
        => token is not null
            && (token.Type == TokenConstants.EOF
                || token.Type == Unipi.MppgParser.Grammar.MppgLexer.NEW_LINE);
}
