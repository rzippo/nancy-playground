using System;
using System.Collections.Generic;
using System.Text;

namespace NancyPlayground;

/// <summary>
/// Handles user input, with a navigable history of previous commands.
/// </summary>
public class LineEditor
{
    private readonly List<string> _history = new List<string>();
    private int _historyIndex = -1;

    /// <summary>
    /// Reads a line from the console with command history support.
    /// Works similarly to Console.ReadLine(), but supports:
    /// - Up/Down arrow to navigate history
    /// - Left/Right to move cursor
    /// - Backspace/Delete/Home/End editing
    /// </summary>
    public string ReadLine()
    {
        var buffer = new StringBuilder();
        int cursor = 0;

        // Starting cursor position (after your prompt)
        int startLeft = Console.CursorLeft;
        int startTop = Console.CursorTop;

        int renderedLength = 0;

        void Render()
        {
            // Go back to where input starts
            Console.SetCursorPosition(startLeft, startTop);

            string text = buffer.ToString();
            Console.Write(text);

            // Clear any leftover characters from previous render
            int extra = renderedLength - text.Length;
            if (extra > 0)
            {
                Console.Write(new string(' ', extra));
            }

            renderedLength = text.Length;

            // Put cursor in correct position
            Console.SetCursorPosition(startLeft + cursor, startTop);
        }

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.SetCursorPosition(startLeft + buffer.Length, startTop);
                    Console.WriteLine(); // Move to next line

                    string line = buffer.ToString();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _history.Add(line);
                        _historyIndex = _history.Count; // one past last
                    }

                    return line;

                case ConsoleKey.Backspace:
                    if (cursor > 0)
                    {
                        buffer.Remove(cursor - 1, 1);
                        cursor--;
                        Render();
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursor < buffer.Length)
                    {
                        buffer.Remove(cursor, 1);
                        Render();
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursor > 0)
                    {
                        cursor--;
                        Console.SetCursorPosition(startLeft + cursor, startTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursor < buffer.Length)
                    {
                        cursor++;
                        Console.SetCursorPosition(startLeft + cursor, startTop);
                    }
                    break;

                case ConsoleKey.Home:
                    cursor = 0;
                    Console.SetCursorPosition(startLeft, startTop);
                    break;

                case ConsoleKey.End:
                    cursor = buffer.Length;
                    Console.SetCursorPosition(startLeft + cursor, startTop);
                    break;

                case ConsoleKey.UpArrow:
                    if (_history.Count > 0)
                    {
                        if (_historyIndex > 0)
                            _historyIndex--;
                        else
                            _historyIndex = 0;

                        buffer.Clear();
                        buffer.Append(_history[_historyIndex]);
                        cursor = buffer.Length;
                        Render();
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_history.Count > 0)
                    {
                        if (_historyIndex < _history.Count - 1)
                        {
                            _historyIndex++;
                            buffer.Clear();
                            buffer.Append(_history[_historyIndex]);
                        }
                        else
                        {
                            // Past the end means "empty new line"
                            _historyIndex = _history.Count;
                            buffer.Clear();
                        }

                        cursor = buffer.Length;
                        Render();
                    }
                    break;

                default:
                    char c = keyInfo.KeyChar;
                    if (c != '\0' && !char.IsControl(c))
                    {
                        buffer.Insert(cursor, c);
                        cursor++;
                        Render();
                    }
                    break;
            }
        }
    }
}
