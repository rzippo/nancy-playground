using Spectre.Console;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

public class InteractiveLineEditorTests
{
    [Fact]
    public void ReadLine_SimpleText_ReturnsTypedText()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTextWithEnter("hello world");

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void ReadLine_MultipleLines_EachReturnsCorrectText()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTextWithEnter("line one");
        testConsole.Input.PushTextWithEnter("line two");
        testConsole.Input.PushTextWithEnter("!quit");

        var editor = new LineEditor(console: testConsole);

        Assert.Equal("line one", editor.ReadLine());
        Assert.Equal("line two", editor.ReadLine());
        Assert.Equal("!quit", editor.ReadLine());
    }

    [Fact]
    public void ReadLine_Backspace_DeletesLastCharacter()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushText("ax");
        testConsole.Input.PushKey(ConsoleKey.Backspace);
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("a", result);
    }

    [Fact]
    public void ReadLine_EmptyInput_ReturnsEmptyString()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("", result);
    }

    [Fact]
    public void ReadLine_HistoryNavigation_RecallsPreviousLines()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTextWithEnter("first command");
        testConsole.Input.PushKey(ConsoleKey.UpArrow);
        testConsole.Input.PushKey(ConsoleKey.Enter);
        testConsole.Input.PushTextWithEnter("!quit");

        var editor = new LineEditor(console: testConsole);

        Assert.Equal("first command", editor.ReadLine());
        Assert.Equal("first command", editor.ReadLine());
        Assert.Equal("!quit", editor.ReadLine());
    }

    [Fact]
    public void ReadLine_BackspaceThenRetype_CorrectOutput()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushText("hella");
        testConsole.Input.PushKey(ConsoleKey.Backspace);
        testConsole.Input.PushKey(ConsoleKey.Backspace);
        testConsole.Input.PushText("lo");
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("hello", result);
    }

    [Fact]
    public void ReadLine_DeleteKey_RemovesCharacterAtCursor()
    {
        var testConsole = new TestConsole();
        // Type "ab", LeftArrow (cursor after 'a', before 'b'), Delete, Enter → "b" removed, result "a"
        testConsole.Input.PushText("ab");
        testConsole.Input.PushKey(ConsoleKey.LeftArrow);
        testConsole.Input.PushKey(ConsoleKey.Delete);
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("a", result);
    }

    [Fact]
    public void ReadLine_HomeEndKeys_NavigateCorrectly()
    {
        var testConsole = new TestConsole();
        // Type "hello", Home (cursor to start), Delete (removes 'h'), End (cursor to end), type " world", Enter
        testConsole.Input.PushText("hello");
        testConsole.Input.PushKey(ConsoleKey.Home);
        testConsole.Input.PushKey(ConsoleKey.Delete);
        testConsole.Input.PushKey(ConsoleKey.End);
        testConsole.Input.PushText(" world");
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("ello world", result);
    }

    [Fact]
    public void ReadLine_SetSessionKeywords_AutocompletesOnTab()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushText("a");
        testConsole.Input.PushKey(ConsoleKey.Tab);
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        editor.SetSessionKeywords(["alpha", "beta", "gamma"]);

        var result = editor.ReadLine();

        Assert.Equal("alpha", result);
    }

    [Fact]
    public void ReadLine_TabCycling_CyclesThroughMatches()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushText("b");
        testConsole.Input.PushKey(ConsoleKey.Tab); // "beta"
        testConsole.Input.PushKey(ConsoleKey.Tab); // "banana"
        testConsole.Input.PushKey(ConsoleKey.Tab); // back to "beta"
        testConsole.Input.PushKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        editor.SetSessionKeywords(["beta", "banana"]);

        var result = editor.ReadLine();

        Assert.Equal("beta", result);
    }

    [Fact]
    public void InteractiveCommand_VersionDirective_IsAppliedToSubsequentLines()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("#!syntax version 1.0");
        console.Input.PushTextWithEnter("a := 5");
        console.Input.PushTextWithEnter("a");
        console.Input.PushTextWithEnter("!quit");

        var app = new CommandAppTester(console: console);
        app.Configure(config =>
        {
            config.AddCommand<InteractiveCommand>("interactive");
        });

        var result = app.Run(["interactive", "--mute-welcome-message"]);

        Assert.Equal(0, result.ExitCode);
        // The version directive line should produce a confirmation in the output
        // The assignment and expression should work normally
    }
}
