using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Unipi.Nancy.Playground.MppgParser.Exceptions;

namespace Unipi.Nancy.Playground.MppgParser;

/// <summary>
/// What a parse does when it reaches a syntax error.
/// </summary>
internal enum ErrorRecovery
{
    /// <summary>
    /// Skips past the error and keeps parsing, so that the statements after a bad one are parsed too.
    /// Collects every error of the input.
    /// </summary>
    CollectAll,

    /// <summary>
    /// Stops at the first error, which is then the earliest failure of the input.
    /// </summary>
    FirstError
}

/// <summary>
/// Records which recovery the parser is reporting, a token it had to invent or one it had to drop.
/// </summary>
/// <remarks>
/// Both are reported with no exception and through the same listener call, so the recovery is read here, where the parser tells the two apart itself, rather than from the wording of the message.
/// It holds only while the report is being made, which is when the listener reads it.
/// </remarks>
internal class RecordingErrorStrategy : DefaultErrorStrategy
{
    /// <summary>
    /// The recovery being reported, or <see cref="ParserRecovery.None"/> outside a report of one.
    /// </summary>
    public ParserRecovery Recovery { get; private set; } = ParserRecovery.None;

    /// <inheritdoc/>
    protected override void ReportMissingToken(Parser recognizer)
    {
        Recovery = ParserRecovery.MissingToken;
        try
        {
            base.ReportMissingToken(recognizer);
        }
        finally
        {
            Recovery = ParserRecovery.None;
        }
    }

    /// <inheritdoc/>
    protected override void ReportUnwantedToken(Parser recognizer)
    {
        Recovery = ParserRecovery.UnwantedToken;
        try
        {
            base.ReportUnwantedToken(recognizer);
        }
        finally
        {
            Recovery = ParserRecovery.None;
        }
    }
}

/// <summary>
/// Stops at the first syntax error, after reporting it to the error listeners.
/// Not <see cref="BailErrorStrategy"/>, which throws before notifying them, leaving no <see cref="SyntaxErrorInfo"/>.
/// </summary>
internal sealed class ReportingBailErrorStrategy : RecordingErrorStrategy
{
    public override void Recover(Parser recognizer, RecognitionException e)
        => throw new ParseCanceledException(e);

    public override IToken RecoverInline(Parser recognizer)
    {
        var e = new InputMismatchException(recognizer);
        ReportError(recognizer, e);
        throw new ParseCanceledException(e);
    }

    public override void Sync(Parser recognizer)
    {
    }
}

/// <summary>
/// The lexer and parser of one MPPG parse, and the errors its listeners collect.
/// </summary>
internal readonly record struct MppgParse(
    Unipi.MppgParser.Grammar.MppgLexer Lexer,
    CommonTokenStream Tokens,
    Unipi.MppgParser.Grammar.MppgParser Parser,
    List<SyntaxErrorInfo> Errors
)
{
    /// <summary>
    /// The version declared by a '#!syntax version' directive, or the default if there is none.
    /// Reads as the default until the input is parsed.
    /// </summary>
    public SyntaxVersion DeclaredSyntaxVersion
    {
        get
        {
            var (major, minor) = Lexer.SyntaxVersion;
            return SyntaxVersion.FromParts(major, minor);
        }
    }

    /// <summary>
    /// Applies <paramref name="rule"/> and reports the first error as a <see cref="SyntaxErrorException"/>.
    /// </summary>
    public T ParseOrThrow<T>(Func<Unipi.MppgParser.Grammar.MppgParser, T> rule)
    {
        try
        {
            var context = rule(Parser);
            ThrowIfErrors();
            return context;
        }
        catch (Exception ex) when (ex is not SyntaxErrorException)
        {
            throw Errors.Count > 0
                ? new SyntaxErrorException(Errors[0].Message, ex) { Error = Errors[0] }
                : ex.WithVersionedKeywordHint(Tokens);
        }
    }

    /// <summary>
    /// Throws the first collected error, if there is one.
    /// </summary>
    public void ThrowIfErrors()
    {
        if (Errors.Count > 0)
            throw new SyntaxErrorException(Errors[0].Message) { Error = Errors[0] };
    }
}

/// <summary>
/// Builds the lexer and parser of an MPPG input.
/// The default error listeners are removed, so an error is collected as a <see cref="SyntaxErrorInfo"/> rather than written to stderr.
/// </summary>
internal static class MppgParsing
{
    /// <param name="text">The input to parse.</param>
    /// <param name="recovery">What to do at the first error.</param>
    /// <param name="syntaxVersion">The version to lex with, or null to take the one the input declares.</param>
    /// <param name="state">The variables whose types seed the parser, when parsing against a session.</param>
    public static MppgParse Create(
        string text,
        ErrorRecovery recovery,
        SyntaxVersion? syntaxVersion = null,
        State? state = null
    )
    {
        var errors = new List<SyntaxErrorInfo>();

        var inputStream = CharStreams.fromString(text);
        var lexer = new Unipi.MppgParser.Grammar.MppgLexer(inputStream);
        if (syntaxVersion is { } version)
            lexer.SetSyntaxVersion(version.Major, version.Minor);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new DiagnosticLexerErrorListener(errors, inputStream));

        var tokens = new CommonTokenStream(lexer);
        var parser = new Unipi.MppgParser.Grammar.MppgParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new DiagnosticParserErrorListener(errors));
        // both strategies record the recovery they report, which is how a missing token is told from an extraneous one
        parser.ErrorHandler = recovery == ErrorRecovery.FirstError
            ? new ReportingBailErrorStrategy()
            : new RecordingErrorStrategy();
        parser.SeedVariableTypes(state);

        return new MppgParse(lexer, tokens, parser, errors);
    }
}
