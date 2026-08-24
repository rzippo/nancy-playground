namespace Unipi.Nancy.Playground.MppgParser.ErrorMatchers;

/// <summary>
/// A plot option written with nothing after its equals sign, as <c>plot(f, out=)</c> is.
/// </summary>
/// <remarks>
/// The name of the option is two tokens back, the equals sign standing between it and where the value should be, so it is read from the line rather than from the rule.
/// </remarks>
internal sealed class PlotOptionValueMatcher : IErrorMatcher<ParserError>
{
    /// <inheritdoc/>
    public string Name => "plot option without a value";

    /// <inheritdoc/>
    public bool Recognises(ParserError error)
        => error.Rule.IsInside("plotOption")
            && error.Tokens.Previous?.Text == "="
            && error.TokenBefore(2) is not null;

    /// <inheritdoc/>
    public RewrittenMessage Write(ParserError error)
        => new($"'{error.TokenBefore(2)!.Text}' is given no value");
}
