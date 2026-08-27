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
    /// What would have fitted, in words, or null where it cannot be put in any.
    /// </summary>
    /// <remarks>
    /// A set of a few tokens is spelled, which is where listing still reads; a larger one is named after the construct it opens, an expression being forty-odd tokens that no reader wants listed.
    /// </remarks>
    public string? InWords => GrammarSets.Naming(Types) ?? Spelled;

    /// <summary>
    /// The tokens listed, where there are few enough of them, each in the terms a reader knows.
    /// </summary>
    private string? Spelled
    {
        get
        {
            if (Names.Count == 0 || Names.Count >= GrammarSets.MinimumToName)
                return null;

            var words = Names.Select(TokenWords.Of).Distinct().ToList();
            return words.Count == 1
                ? words[0]
                : string.Join(", ", words.SkipLast(1)) + " or " + words[^1];
        }
    }

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
/// The neighbours say what was being written where the offending token alone does not: a keyword used as a name is reported at the keyword in <c>div := 3</c> and at the <c>:=</c> in <c>star := 3</c>, so both are needed to recognise the one mistake.
/// </remarks>
/// <param name="Offending">The token that could not be used.</param>
/// <param name="Previous">The token before it, or null where it opens the input.</param>
/// <param name="Next">The token after it, or null where it ends the input.</param>
internal sealed record ErrorTokens(IToken? Offending, IToken? Previous, IToken? Next);

/// <summary>
/// The rule being parsed when the error was found.
/// </summary>
/// <param name="Name">Its name, which is the last of <paramref name="Stack"/>.</param>
/// <param name="Stack">The rules it was nested in, outermost first.</param>
/// <param name="StartToken">
/// Its first token, which is the name of the construct being written where the rule names of the grammar are internal: <c>bucket</c> where the rule is called tokenBucket.
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
/// The message layer turns these into what a user is shown, see <see cref="SyntaxErrorMessages"/>.
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

            var assignment = AssignmentOutsideAString(line);
            if (assignment < 0 || Position.Column >= assignment)
                return false;

            // what stands before the assignment is the name being written, so nothing else can stand there
            return line[..assignment]
                .Where((_, column) => column != Position.Column)
                .All(character => char.IsLetterOrDigit(character)
                    || character is '_' or '-'
                    || char.IsWhiteSpace(character));
        }
    }

    /// <summary>
    /// Where the assignment of <paramref name="line"/> is, or -1 where the line has none.
    /// </summary>
    /// <remarks>
    /// A <c>:=</c> inside a string is text rather than an assignment, as the one in <c>@ + "a := b"</c> is, so the quotes are followed while scanning.
    /// The string is read from the line and not from the tokens, there being none: the lexer stopped before it could make any.
    /// </remarks>
    private static int AssignmentOutsideAString(string line)
    {
        var insideAString = false;
        for (var i = 0; i < line.Length - 1; i++)
        {
            if (line[i] == '"')
                insideAString = !insideAString;
            else if (!insideAString && line[i] == ':' && line[i + 1] == '=')
                return i;
        }

        return -1;
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
/// <param name="LineTokens">The tokens of the line the error is on, which is where a call the rule stack lost is read from.</param>
/// <param name="ReadableMessage">
/// The same, with the quoted input taken from the source rather than from the tokens joined together, or null where there is nothing to repair.
/// ANTLR writes <c>(floorcomp</c> where the script reads <c>( floor comp</c>, so this is what a reader is shown while <paramref name="ParserMessage"/> stays what was said.
/// </param>
/// <param name="DefaultHint">What to add to the message, where something is known beyond it.</param>
/// <param name="Version">The syntax version the input was read with, which decides the words that are keywords in it.</param>
internal sealed record ParserError(
    SourcePosition Position,
    ErrorTokens Tokens,
    ParsedRule Rule,
    ExpectedTokens Expected,
    RecognitionException? Exception,
    IReadOnlyDictionary<string, Unipi.MppgParser.Grammar.MppgParser.VariableType> DeclaredVariables,
    string ParserMessage,
    string? DefaultHint = null,
    string? ReadableMessage = null,
    ParserRecovery Recovery = ParserRecovery.None,
    IReadOnlyList<IToken>? LineTokens = null,
    SyntaxVersion? Version = null
) : ParseError(Position, ReadableMessage ?? ParserMessage, DefaultHint)
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

    /// <summary>
    /// The operation of a later version the error is about, with the version that introduced it, or null where it is about none.
    /// </summary>
    /// <remarks>
    /// Under an older version the word is not a keyword, so it lexes as a name and fails as one: at the token itself in <c>abs(x)</c>, and inside the call in <c>pow(x, 2)</c>, which are the two places read here.
    /// A name the program declares is its own, whatever it spells.
    /// </remarks>
    public (string Keyword, SyntaxVersion IntroducedIn)? KeywordOfALaterVersion
        => Version is not { } inForce
            ? null
            : OperationOfALaterVersion(Tokens.Offending, inForce)
                ?? OperationOfALaterVersion(EnclosingCall, inForce);

    private (string Keyword, SyntaxVersion IntroducedIn)? OperationOfALaterVersion(IToken? token, SyntaxVersion inForce)
        => token is { } named
            && named.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
            && !IsDeclared(named.Text)
            && VersionedKeywords.IntroducedIn.TryGetValue(named.Text, out var introducedIn)
            && introducedIn > inForce
                ? (named.Text, introducedIn)
                : null;

    /// <summary>
    /// The call the offending token stands inside, by the name it is written with, or null where it stands in none.
    /// </summary>
    /// <remarks>
    /// Read from the tokens of the line rather than from the rule being parsed: prediction leaves the rule of a scalar call behind, so <c>pow(2)</c> fails with <c>expression</c> on the stack and nothing on it saying which call it was.
    /// The brackets are counted back from the offending token, and the name is what stands before the one still open.
    /// </remarks>
    public IToken? EnclosingCall
    {
        get
        {
            if (Tokens.Offending is not { } offending || LineTokens is not { Count: > 0 } tokens)
                return null;

            var at = -1;
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].TokenIndex == offending.TokenIndex)
                {
                    at = i;
                    break;
                }
            }

            if (at < 0)
                return null;

            var depth = 0;
            for (var i = at - 1; i >= 0; i--)
            {
                var text = tokens[i].Text;
                if (text == ")")
                    depth++;
                else if (text == "(")
                {
                    if (depth == 0)
                        return i > 0 && IsNameLike(tokens[i - 1]) ? tokens[i - 1] : null;
                    depth--;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// True where the token reads as the name of a call, i.e. a keyword or a variable, both of which are written the same way in front of a bracket.
    /// </summary>
    private static bool IsNameLike(IToken token)
        => TokenFacts.IsKeywordSpelledLikeAName(token)
            || token.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER;

    /// <summary>
    /// The token <paramref name="steps"/> places before the offending one on its line, or null where the line does not reach that far back.
    /// </summary>
    /// <remarks>
    /// The two beside the offending token are on <see cref="Tokens"/>; this reaches further, for what is written before them, e.g. the name of the plot option in <c>out=)</c>.
    /// </remarks>
    public IToken? TokenBefore(int steps)
    {
        if (Tokens.Offending is not { } offending || LineTokens is not { Count: > 0 } tokens)
            return null;

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].TokenIndex == offending.TokenIndex)
                return i >= steps ? tokens[i - steps] : null;
        }

        return null;
    }

    /// <summary>
    /// How many round brackets the line opens and closes, which are equal in a line that is written whole.
    /// </summary>
    /// <remarks>
    /// Only the round ones: a square bracket says which end of a segment is included, so <c>](0, 1) 1 (1, 2)[</c> is written with two that never match, while its round ones do.
    /// Read from the line rather than from its tokens, which are only buffered as far as the parser has read, and with the strings and the comment skipped so that a bracket written inside one is text.
    /// </remarks>
    public (int Opened, int Closed) RoundBrackets
    {
        get
        {
            if (Position.SourceLine is not { } line)
                return (0, 0);

            var opened = 0;
            var closed = 0;
            var insideAString = false;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                    insideAString = !insideAString;
                else if (insideAString)
                    continue;
                else if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/')
                    break;
                else if (line[i] == '(')
                    opened++;
                else if (line[i] == ')')
                    closed++;
            }

            return (opened, closed);
        }
    }

    /// <summary>
    /// True where the line opens more round brackets than it closes, or the other way round, which is a mistake wherever the parser happens to stop.
    /// </summary>
    public bool HasUnbalancedBrackets => RoundBrackets.Opened != RoundBrackets.Closed;

    /// <summary>
    /// What to suggest looking at where the brackets of the line are not balanced, or null where they are.
    /// </summary>
    /// <remarks>
    /// A bracket left out is reported wherever the parser gives up, which is rarely where it was left out, so the count is worth saying whatever the message is about.
    /// </remarks>
    public string? BracketHint
        => HasUnbalancedBrackets
            ? $"The brackets of this line are not balanced: {RoundBrackets.Opened} opened and {RoundBrackets.Closed} closed."
            : null;

    /// <summary>
    /// True where the line opens with a name and an <c>=</c>, which is an assignment written with the operator of a comparison.
    /// </summary>
    /// <remarks>
    /// Only where the name opens the line: an <c>=</c> after a name is a comparison inside an assertion and an option inside a plot, so those are left to what knows about them.
    /// What the mistake was is otherwise rarely worth guessing, a name and an expression with nothing between them saying only that something is missing.
    /// </remarks>
    public bool IsAssignmentWrittenWithAnEquals => NameAssignedWithAnEquals is not null;

    /// <summary>
    /// The name the line assigns to with an <c>=</c>, or null where the line is not one.
    /// </summary>
    /// <remarks>
    /// The error lands on either side of the operator, on the name where the line is read on its own and on the <c>=</c> where the lines after it give the parser somewhere else to go, so whichever it did not land on is read.
    /// </remarks>
    public IToken? NameAssignedWithAnEquals
    {
        get
        {
            if (Rule.IsInside("plotArg") || Rule.IsInside("assertion"))
                return null;

            // the error is on the name, and the '=' follows it
            if (Tokens.Offending is { } token
                && token.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
                && Tokens.Next?.Text == "="
                && (Tokens.Previous is null || TokenFacts.EndsTheLine(Tokens.Previous)))
                return token;

            // the error is on the '=', and the name opens the line before it
            if (Tokens.Offending?.Text == "="
                && Tokens.Previous is { } name
                && name.Type == Unipi.MppgParser.Grammar.MppgLexer.IDENTIFIER
                && TokenBefore(2) is null)
                return name;

            return null;
        }
    }

    /// <summary>
    /// The keyword the line is trying to use as a name, or null where no name is being written.
    /// </summary>
    /// <remarks>
    /// The assignment that follows the keyword is what says a name was meant, and the error lands on either side of it: on the keyword in <c>div := 3</c> and on the <c>:=</c> in <c>star := 3</c>.
    /// A fact rather than a matcher's own reading, since one matcher says so and another has to keep quiet about it.
    /// </remarks>
    public IToken? KeywordBeingNamed
    {
        get
        {
            if (TokenFacts.IsKeywordSpelledLikeAName(Tokens.Offending)
                && Tokens.Next?.Type == Unipi.MppgParser.Grammar.MppgLexer.ASSIGN)
                return Tokens.Offending;

            if (Tokens.Offending?.Type == Unipi.MppgParser.Grammar.MppgLexer.ASSIGN
                && TokenFacts.IsKeywordSpelledLikeAName(Tokens.Previous))
                return Tokens.Previous;

            return null;
        }
    }
}

/// <summary>
/// A <c>#!syntax version</c> directive this build cannot apply, which the playground reports itself rather than the parser, the directive being well formed as far as the grammar is concerned.
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
    /// True if <paramref name="token"/> is a keyword spelled the way a name is, i.e. one that a user can mean as a variable: <c>div</c> and <c>star</c> are, <c>:=</c> and <c>(</c> are not.
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
