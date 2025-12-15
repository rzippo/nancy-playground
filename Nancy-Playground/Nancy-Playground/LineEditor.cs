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
    /// - Left/Right, Home, End, Backspace, Delete
    /// - Ctrl+Left / Ctrl+Right to move by word
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

        int FindPreviousWordStart(int position)
        {
            if (position <= 0 || buffer.Length == 0)
                return 0;

            int i = position - 1;

            // Skip non-word characters immediately to the left
            while (i >= 0 && !IsWordChar(buffer[i]))
                i--;

            // Move left until start of word
            while (i >= 0 && IsWordChar(buffer[i]))
                i--;

            return Math.Max(i + 1, 0);
        }

        int FindNextWordEnd(int position)
        {
            int len = buffer.Length;
            if (position >= len || len == 0)
                return len;

            int i = position;

            // Skip non-word characters immediately to the right
            while (i < len && !IsWordChar(buffer[i]))
                i++;

            // Move right until end of word
            while (i < len && IsWordChar(buffer[i]))
                i++;

            return i;
        }

        bool IsWordChar(char c)
        {
            char[] punctuation = [ '.', ',', ';', ':', '!', '?', '-', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '\'', '\"' ];
            return !char.IsWhiteSpace(c) && !punctuation.Contains(c);
        }

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            bool ctrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;

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
                    if (ctrl)
                    {
                        int newPos = FindPreviousWordStart(cursor);
                        if (newPos != cursor)
                        {
                            cursor = newPos;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    else
                    {
                        if (cursor > 0)
                        {
                            cursor--;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (ctrl)
                    {
                        int newPos = FindNextWordEnd(cursor);
                        if (newPos != cursor)
                        {
                            cursor = newPos;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
                    }
                    else
                    {
                        if (cursor < buffer.Length)
                        {
                            cursor++;
                            Console.SetCursorPosition(startLeft + cursor, startTop);
                        }
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

    /// <summary>
    /// Removes a history entry at the given index.
    /// Returns true if removed, false if index was invalid.
    /// </summary>
    public bool RemoveHistoryAt(int index)
    {
        if (index < 0 || index >= _history.Count)
            return false;

        _history.RemoveAt(index);

        // Reset history index to "one past the end"
        _historyIndex = _history.Count;
        return true;
    }

    /// <summary>
    /// Removes a history entry by value.
    /// If removeAll is false, removes only the first match.
    /// Returns true if at least one entry was removed.
    /// </summary>
    public bool RemoveHistory(string value, bool removeAll = false)
    {
        if (value == null)
            return false;

        bool removed = false;

        if (removeAll)
        {
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_history[i], value, StringComparison.Ordinal))
                {
                    _history.RemoveAt(i);
                    removed = true;
                }
            }
        }
        else
        {
            int idx = _history.FindIndex(h => string.Equals(h, value, StringComparison.Ordinal));
            if (idx >= 0)
            {
                _history.RemoveAt(idx);
                removed = true;
            }
        }

        if (removed)
        {
            _historyIndex = _history.Count;
        }

        return removed;
    }
}
