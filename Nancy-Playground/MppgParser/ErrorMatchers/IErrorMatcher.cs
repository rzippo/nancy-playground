namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// One kind of error recognised, and what to write about it.
/// </summary>
/// <remarks>
/// Recognising and writing are kept apart so that both can be asked separately: a test enumerates the registry and checks that no two matchers recognise the same error, which is the mistake a matcher written with too loose a guard makes.
/// One matcher to a file, so that what it recognises, what it writes and what it needs to decide are read together.
/// </remarks>
/// <typeparam name="TError">The kind of error it reads, which is what keeps a matcher of the parser from being handed one of the lexer.</typeparam>
internal interface IErrorMatcher<in TError> where TError : ParseError
{
    /// <summary>
    /// What it recognises, in a few words, which the verbose output names so that a message can be traced back to what wrote it.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether <paramref name="error"/> is the one it knows about.
    /// </summary>
    bool Recognises(TError error);

    /// <summary>
    /// What to say about <paramref name="error"/>, asked only where it recognises it.
    /// </summary>
    RewrittenMessage Write(TError error);
}
