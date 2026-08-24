namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// An interval written without one of its extremes, as <c>xlim=[1,]</c> and <c>xlim=[,2]</c> are.
/// </summary>
/// <remarks>
/// The rule says what is being written where the expected set only says that a number was wanted, so the message names the extreme rather than the number.
/// </remarks>
internal sealed class IntervalEndMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "interval missing an extreme";

    /// <inheritdoc/>
    public bool Recognises(ParserError error) => MissingExtreme(error) is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"the interval is missing its {MissingExtreme(error)} extreme");

    /// <summary>
    /// Which extreme was left out, read from the comma: nothing before it is the left one, nothing after it the right.
    /// </summary>
    private static string? MissingExtreme(ParserError error)
    {
        if (!error.Rule.IsInside("interval"))
            return null;

        if (error.Tokens.Offending?.Text == "]" && error.Tokens.Previous?.Text == ",")
            return "right";

        if (error.Tokens.Offending?.Text == "," && error.Tokens.Previous?.Text == "[")
            return "left";

        return null;
    }
}
