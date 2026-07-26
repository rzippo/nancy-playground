using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

/// <summary>
/// Pushes input to a <see cref="TestConsoleInput"/> as a real console would deliver it.
/// </summary>
/// <remarks>
/// <see cref="TestConsoleInput.PushText"/> derives the key from the character code, so typed punctuation arrives as a navigation key: '(' as DownArrow, '.' as Delete, '%' as LeftArrow, '#' as End.
/// <see cref="TestConsoleInput.PushKey(ConsoleKey)"/> has the dual problem, deriving a printable character from the key code.
/// These methods push the pair of key and character that a console sends, so that any line can be typed.
/// </remarks>
public static class TestConsoleInputExtensions
{
    /// <summary>
    /// Pushes a single typed character.
    /// </summary>
    public static void PushTypedChar(this TestConsoleInput input, char c)
    {
        var shift = char.IsUpper(c);
        input.PushKey(new ConsoleKeyInfo(c, LogicalKey(c), shift, alt: false, control: false));
    }

    /// <summary>
    /// Pushes a typed text, without submitting it.
    /// </summary>
    public static void PushTypedText(this TestConsoleInput input, string text)
    {
        foreach (var c in text)
            input.PushTypedChar(c);
    }

    /// <summary>
    /// Pushes a typed line, then submits it with Enter.
    /// </summary>
    public static void PushTypedLine(this TestConsoleInput input, string line)
    {
        input.PushTypedText(line);
        input.PushEditingKey(ConsoleKey.Enter);
    }

    /// <summary>
    /// Pushes an editing or navigation key, such as an arrow or Delete.
    /// </summary>
    public static void PushEditingKey(this TestConsoleInput input, ConsoleKey key)
    {
        var keyChar = key switch
        {
            ConsoleKey.Enter => '\r',
            ConsoleKey.Tab => '\t',
            ConsoleKey.Backspace => '\b',
            // arrows, Delete, Home and End carry no character
            _ => '\0'
        };
        input.PushKey(new ConsoleKeyInfo(keyChar, key, shift: false, alt: false, control: false));
    }

    /// <summary>
    /// The key a console reports for a typed character.
    /// </summary>
    /// <remarks>
    /// Punctuation is reported as <see cref="ConsoleKey.None"/>, as its actual key depends on the keyboard layout.
    /// </remarks>
    private static ConsoleKey LogicalKey(char c)
    {
        var upper = char.ToUpperInvariant(c);
        return upper switch
        {
            >= 'A' and <= 'Z' => ConsoleKey.A + (upper - 'A'),
            >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
            ' ' => ConsoleKey.Spacebar,
            _ => ConsoleKey.None
        };
    }
}
