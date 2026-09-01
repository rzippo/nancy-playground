using Antlr4.Runtime;

namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// What a matcher writes: the message says what is wrong, the hint adds the detail behind it.
/// </summary>
/// <remarks>
/// The message names what the user was writing, e.g. a variable name, where the hint may name what the parser found, e.g. a character it does not support.
/// The message is a fragment, printed after the position, so it neither opens with a capital nor closes with a period.
/// The hint is a sentence.
/// </remarks>
/// <param name="Message">What to show in place of what the error carries.</param>
/// <param name="Hint">What to add below it, where there is more to say.</param>
/// <param name="Position">
/// Where to point the caret, when the token the message is about is not the one the parser stopped
/// at, e.g. the name of a call named deep inside its argument list. Null keeps the parser's own
/// position.
/// </param>
/// <param name="WrittenBy">
/// The matcher that wrote it, filled in by the registry rather than by the matcher, and named by the verbose output.
/// </param>
internal sealed record RewrittenMessage(string Message, string? Hint = null, IToken? Position = null, string? WrittenBy = null);
