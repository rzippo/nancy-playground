namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Reads whole lines, for input that is piped in rather than typed, e.g. `cat script.mppg | nancy-playground interactive`.
/// </summary>
/// <remarks>
/// The line editor cannot be used then, as reading keys requires a terminal.
/// Nothing is echoed here: the statement formatter does it, through the echo setting.
/// </remarks>
public class PipedLineReader : ILineReader
{
    private readonly TextReader _input;

    /// <param name="input">Where the lines are read from. Taken as a parameter so it can be a test one.</param>
    public PipedLineReader(TextReader input)
    {
        _input = input;
    }

    public string? ReadLine()
        => _input.ReadLine();

    /// Autocomplete needs a terminal.
    public void SetSessionKeywords(IEnumerable<string> sessionKeywords)
    {
    }

    /// History is navigated with the arrow keys, which needs a terminal.
    public void AddToHistory(IEnumerable<string> lines)
    {
    }

    /// <inheritdoc cref="AddToHistory"/>
    public void ClearHistory()
    {
    }
}
