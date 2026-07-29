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
        testConsole.Input.PushTypedLine("hello world");

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("hello world", result);
    }

    [Theory]
    // characters whose code collides with a navigation key: '(' with DownArrow, ')' with Select, '.' with Delete
    [InlineData("f := ratency(1, 3)")]
    [InlineData("plotTikz(f, xlim = [-0.3, 15], out = \"chart.tex\")")]
    [InlineData("hdev(ac, sc)")]
    // '%' collides with LeftArrow, '#' with End, '$' with Home, '&' with UpArrow
    [InlineData("% a comment")]
    [InlineData("#!syntax version 1.1")]
    [InlineData("!load /tmp/script.mppg")]
    [InlineData("f := uaf( [(0,0)1(+inf,+inf)[ )")]
    public void ReadLine_MppgSyntax_ReturnsTypedText(string typed)
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedLine(typed);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal(typed, result);
    }

    [Fact]
    public void ReadLine_MultipleLines_EachReturnsCorrectText()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedLine("line one");
        testConsole.Input.PushTypedLine("line two");
        testConsole.Input.PushTypedLine("!quit");

        var editor = new LineEditor(console: testConsole);

        Assert.Equal("line one", editor.ReadLine());
        Assert.Equal("line two", editor.ReadLine());
        Assert.Equal("!quit", editor.ReadLine());
    }

    [Fact]
    public void ReadLine_Backspace_DeletesLastCharacter()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedText("ax");
        testConsole.Input.PushEditingKey(ConsoleKey.Backspace);
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("a", result);
    }

    [Fact]
    public void ReadLine_EmptyInput_ReturnsEmptyString()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("", result);
    }

    [Fact]
    public void ReadLine_HistoryNavigation_RecallsPreviousLines()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedLine("first command");
        testConsole.Input.PushEditingKey(ConsoleKey.UpArrow);
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);
        testConsole.Input.PushTypedLine("!quit");

        var editor = new LineEditor(console: testConsole);

        Assert.Equal("first command", editor.ReadLine());
        Assert.Equal("first command", editor.ReadLine());
        Assert.Equal("!quit", editor.ReadLine());
    }

    [Fact]
    public void ReadLine_BackspaceThenRetype_CorrectOutput()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedText("hella");
        testConsole.Input.PushEditingKey(ConsoleKey.Backspace);
        testConsole.Input.PushEditingKey(ConsoleKey.Backspace);
        testConsole.Input.PushTypedText("lo");
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("hello", result);
    }

    [Fact]
    public void ReadLine_DeleteKey_RemovesCharacterAtCursor()
    {
        var testConsole = new TestConsole();
        // Type "ab", LeftArrow (cursor after 'a', before 'b'), Delete, Enter → "b" removed, result "a"
        testConsole.Input.PushTypedText("ab");
        testConsole.Input.PushEditingKey(ConsoleKey.LeftArrow);
        testConsole.Input.PushEditingKey(ConsoleKey.Delete);
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("a", result);
    }

    [Fact]
    public void ReadLine_HomeEndKeys_NavigateCorrectly()
    {
        var testConsole = new TestConsole();
        // Type "hello", Home (cursor to start), Delete (removes 'h'), End (cursor to end), type " world", Enter
        testConsole.Input.PushTypedText("hello");
        testConsole.Input.PushEditingKey(ConsoleKey.Home);
        testConsole.Input.PushEditingKey(ConsoleKey.Delete);
        testConsole.Input.PushEditingKey(ConsoleKey.End);
        testConsole.Input.PushTypedText(" world");
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        var result = editor.ReadLine();

        Assert.Equal("ello world", result);
    }

    [Fact]
    public void ReadLine_SetSessionKeywords_AutocompletesOnTab()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedText("a");
        testConsole.Input.PushEditingKey(ConsoleKey.Tab);
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        editor.SetSessionKeywords(["alpha", "beta", "gamma"]);

        var result = editor.ReadLine();

        Assert.Equal("alpha", result);
    }

    [Fact]
    public void ReadLine_TabCycling_CyclesThroughMatches()
    {
        var testConsole = new TestConsole();
        testConsole.Input.PushTypedText("b");
        testConsole.Input.PushEditingKey(ConsoleKey.Tab); // "beta"
        testConsole.Input.PushEditingKey(ConsoleKey.Tab); // "banana"
        testConsole.Input.PushEditingKey(ConsoleKey.Tab); // back to "beta"
        testConsole.Input.PushEditingKey(ConsoleKey.Enter);

        var editor = new LineEditor(console: testConsole);
        editor.SetSessionKeywords(["beta", "banana"]);

        var result = editor.ReadLine();

        Assert.Equal("beta", result);
    }

    [Fact]
    public void InteractiveCommand_VersionDirective_IsAppliedToSubsequentLines()
    {
        var console = new TestConsole();
        // drives the line editor, as a user typing at a terminal would
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTypedLine("#!syntax version 1.0");
        console.Input.PushTypedLine("a := 5");
        console.Input.PushTypedLine("a");
        console.Input.PushTypedLine("!quit");

        var app = new CommandAppTester(console: console);
        app.Configure(config =>
        {
            config.AddCommand<InteractiveCommand>("interactive");
        });

        var result = app.Run(["interactive", "--mute-welcome-message"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Syntax version set to 1.0.", console.Output);
        Assert.Contains("a = 5", console.Output);
    }
}
