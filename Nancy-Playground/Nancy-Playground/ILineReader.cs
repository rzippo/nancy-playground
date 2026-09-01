namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// Source of the lines of an interactive session.
/// </summary>
/// <remarks>
/// Typing at a terminal and reading piped input differ in what they can offer: history, autocomplete
/// and editing keys only make sense for the former, and are no-ops for the latter.
/// </remarks>
public interface ILineReader
{
    /// <summary>
    /// Reads the next line, or returns null once the input ended.
    /// </summary>
    string? ReadLine();

    /// <summary>
    /// Sets the names defined in the session, for autocomplete.
    /// </summary>
    void SetSessionKeywords(IEnumerable<string> sessionKeywords);

    /// <summary>
    /// Replaces the base keyword list, e.g. when the declared syntax version changes what applies.
    /// </summary>
    void SetKeywords(IEnumerable<string> keywords);

    /// <summary>
    /// Adds lines to the history that can be navigated.
    /// </summary>
    void AddToHistory(IEnumerable<string> lines);

    /// <summary>
    /// Forgets the history.
    /// </summary>
    void ClearHistory();
}
