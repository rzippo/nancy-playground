using System.Text;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

/// <summary>
/// Tests interactive mode commands, driving the line editor with simulated typing.
/// </summary>
public class InteractiveCommandTests
{
    [Fact]
    public void Quit_PrintsGoodbye()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("!quit");

        var exitCode = RunInteractive(console);

        Assert.Equal(0, exitCode);
        Assert.Contains("Bye.", console.Output);
    }

    [Fact]
    public void Assignment_PrintsAssignedVariable()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("f = new RateLatencyServiceCurve(1, 3)", console.Output);
    }

    [Fact]
    public void PlotTikz_PrintsTikzCode()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("\\begin{tikzpicture}", console.Output);
        Assert.Contains("\\end{tikzpicture}", console.Output);
    }

    [Fact]
    public void PlotTikz_WithOut_WritesCodeToFile()
    {
        // in interactive mode, plots are saved in the current directory
        var outName = $"interactive-plot-{Guid.NewGuid():N}";
        var codePath = Path.Combine(Environment.CurrentDirectory, $"{outName}.tex");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine($"plotTikz(f, out = \"{outName}.png\")");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console);

            // the wrong extension is replaced, and the confirmation reports the file actually written
            Assert.True(File.Exists(codePath), $"TikZ code not written to: {codePath}");
            Assert.Contains($"{outName}.tex", console.Output);
            Assert.Contains("\\begin{tikzpicture}", File.ReadAllText(codePath));
        }
        finally
        {
            if (File.Exists(codePath))
                File.Delete(codePath);
        }
    }

    [Fact]
    public void VersionDirectiveV1_0_IsAppliedToSubsequentLines()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("#!syntax version 1.0");
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Syntax version set to 1.0.", console.Output);
        // plotTikz requires syntax version 1.1
        Assert.Contains("Syntax error:", console.Output);
        Assert.DoesNotContain("\\begin{tikzpicture}", console.Output);
    }

    [Fact]
    public void VersionDirectiveV1_1_AllowsPlotTikz()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("#!syntax version 1.1");
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Syntax version set to 1.1.", console.Output);
        Assert.DoesNotContain("Syntax error:", console.Output);
        Assert.Contains("\\begin{tikzpicture}", console.Output);
    }

    [Fact]
    public void SecondVersionDirective_IsRejectedAsDuplicate()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("#!syntax version 1.1");
        console.Input.PushTypedLine("#!syntax version 1.0");
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Syntax version set to 1.1.", console.Output);
        Assert.Contains("Duplicate syntax version directive.", console.Output);
        // the first directive stays in effect, hence plotTikz is still allowed
        Assert.DoesNotContain("Syntax error:", console.Output);
        Assert.Contains("\\begin{tikzpicture}", console.Output);
    }

    [Fact]
    public void VersionDirectiveAfterAStatement_IsRejected()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("#!syntax version 1.0");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        // the exported program would ignore the directive too, as it would not be in the preamble
        Assert.Contains("only applied before any other statement", console.Output);
        Assert.Contains("Active version: 1.3.", console.Output);
        Assert.Contains("\\begin{tikzpicture}", console.Output);
    }

    [Fact]
    public void Clear_ReleasesTheSyntaxVersion()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("#!syntax version 1.0");
        console.Input.PushTypedLine("!clear");
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        // after clearing, the session is back to the default version, which allows plotTikz
        Assert.Contains("Syntax version set to 1.0.", console.Output);
        Assert.DoesNotContain("Syntax error:", console.Output);
        Assert.Contains("\\begin{tikzpicture}", console.Output);
    }

    [Fact]
    public void Clear_AllowsANewVersionDirective()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("#!syntax version 1.1");
        console.Input.PushTypedLine("!clear");
        console.Input.PushTypedLine("#!syntax version 1.0");
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Syntax version set to 1.0.", console.Output);
        Assert.DoesNotContain("Duplicate syntax version directive.", console.Output);
        // version 1.0 is now in effect, hence plotTikz is rejected
        Assert.Contains("Syntax error:", console.Output);
    }

    [Fact]
    public void Comment_IsEchoed()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("% a comment");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("% a comment", console.Output);
    }

    [Fact]
    public void Help_PrintsSections()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("!help");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Plots", console.Output);
        Assert.Contains("plotTikz", console.Output);
    }

    [Fact]
    public void HelpWithQuery_PrintsMatchingItemsOnly()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("!help tikz");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("plotTikz", console.Output);
        Assert.DoesNotContain("Asserts", console.Output);
    }

    [Fact]
    public void HelpWithUnknownQuery_ReportsNoMatch()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("!help notakeyword");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("No match found for the given keywords.", console.Output);
    }

    [Fact]
    public void Load_ExecutesProgramFromFile()
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");
        File.WriteAllText(scriptPath,
            """
            f := ratency(1, 3)
            g := bucket(2, 1)
            hdev(g, f)
            """);

        var console = CreateConsole();
        console.Input.PushTypedLine($"!load {scriptPath}");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console);

            Assert.Contains("Program loaded:", console.Output);
            Assert.Contains("f = new RateLatencyServiceCurve(1, 3)", console.Output);
        }
        finally
        {
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
        }
    }

    [Fact]
    public void Load_AppliesVersionDirectiveOfALoadedProgram()
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");
        File.WriteAllText(scriptPath,
            """
            #!syntax version 1.0
            f := ratency(1, 3)
            """);

        var console = CreateConsole();
        console.Input.PushTypedLine($"!load {scriptPath}");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console);

            Assert.Contains("Syntax version set to 1.0.", console.Output);
            // the loaded version holds for the rest of the session, hence plotTikz is rejected
            Assert.Contains("Syntax error:", console.Output);
        }
        finally
        {
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
        }
    }

    [Fact]
    public void Load_RejectsVersionDirectiveAfterASessionStatement()
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");
        File.WriteAllText(scriptPath,
            """
            #!syntax version 1.0
            g := bucket(2, 1)
            """);

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine($"!load {scriptPath}");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console);

            Assert.Contains("only applied before any other statement", console.Output);
            Assert.Contains("\\begin{tikzpicture}", console.Output);
        }
        finally
        {
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
        }
    }

    [Fact]
    public void LoadMissingFile_ReportsError()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");

        var console = CreateConsole();
        console.Input.PushTypedLine($"!load {missingPath}");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("not found", console.Output);
    }

    [Fact]
    public void Export_WritesProgramToFile()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine($"!export {outputPath}");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console);

            Assert.Contains("exported successfully", console.Output);
            Assert.Contains("ratency", File.ReadAllText(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Convert_WritesNancyProgramToFile()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.cs");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f)");
        console.Input.PushTypedLine($"!convert {outputPath}");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console);

            Assert.Contains("converted successfully", console.Output);
            var code = File.ReadAllText(outputPath);
            Assert.Contains("new RateLatencyServiceCurve", code);
            Assert.Contains("TikzPlots.ToTikzPlotCode(", code);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    // The export root, --export-root, is the directory the files written by the session are saved in.

    [Fact]
    public void Export_RelativePath_IsResolvedAgainstTheExportRoot()
    {
        var exportRoot = CreateTempDirectory();
        var outputPath = Path.Combine(exportRoot, "session.mppg");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("!export session.mppg");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console, exportRoot);

            Assert.True(File.Exists(outputPath), $"Program not exported to: {outputPath}");
            Assert.Contains("ratency", File.ReadAllText(outputPath));
            // the confirmation reports where the file was actually written
            Assert.Contains(outputPath, console.Output);
        }
        finally
        {
            Directory.Delete(exportRoot, true);
        }
    }

    [Fact]
    public void Export_AbsolutePath_IgnoresTheExportRoot()
    {
        var exportRoot = CreateTempDirectory();
        var elsewhere = CreateTempDirectory();
        var outputPath = Path.Combine(elsewhere, "session.mppg");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine($"!export {outputPath}");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console, exportRoot);

            Assert.True(File.Exists(outputPath), $"Program not exported to: {outputPath}");
            Assert.Empty(Directory.GetFiles(exportRoot));
        }
        finally
        {
            Directory.Delete(exportRoot, true);
            Directory.Delete(elsewhere, true);
        }
    }

    [Fact]
    public void Convert_RelativePath_IsResolvedAgainstTheExportRoot()
    {
        var exportRoot = CreateTempDirectory();
        var outputPath = Path.Combine(exportRoot, "session.cs");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("!convert session.cs");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console, exportRoot);

            Assert.True(File.Exists(outputPath), $"Program not converted to: {outputPath}");
            Assert.Contains("new RateLatencyServiceCurve", File.ReadAllText(outputPath));
        }
        finally
        {
            Directory.Delete(exportRoot, true);
        }
    }

    [Fact]
    public void PlotTikz_WithOut_IsSavedInTheExportRoot()
    {
        var exportRoot = CreateTempDirectory();
        var codePath = Path.Combine(exportRoot, "plot.tex");

        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("plotTikz(f, out = \"plot.tex\")");
        console.Input.PushTypedLine("!quit");

        try
        {
            RunInteractive(console, exportRoot);

            Assert.True(File.Exists(codePath), $"TikZ code not written to: {codePath}");
            Assert.Contains("\\begin{tikzpicture}", File.ReadAllText(codePath));
        }
        finally
        {
            Directory.Delete(exportRoot, true);
        }
    }

    [Fact]
    public void ExportRoot_ThatDoesNotExist_StopsBeforeTheSession()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}");

        var console = CreateConsole();
        console.Input.PushTypedLine("!quit");

        var exitCode = RunInteractive(console, missingRoot);

        Assert.Equal(1, exitCode);
        Assert.Contains("directory not found", console.Output);
        // the session never started, so the quit was not read
        Assert.DoesNotContain("Bye.", console.Output);
    }

    [Fact]
    public void Clear_ResetsVariables()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1, 3)");
        console.Input.PushTypedLine("!clear");
        console.Input.PushTypedLine("f");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Session cleared.", console.Output);
        // f no longer exists, hence it cannot be parsed as a variable
        Assert.Contains("Syntax error:", console.Output);
    }

    [Fact]
    public void SyntaxError_ReportsTheOffendingLine()
    {
        var console = CreateConsole();
        console.Input.PushTypedLine("f := ratency(1,");
        console.Input.PushTypedLine("!quit");

        RunInteractive(console);

        Assert.Contains("Syntax error:", console.Output);
        Assert.Contains("f := ratency(1,", console.Output);
    }

    // Piped input, e.g. `cat script.mppg | nancy-playground interactive`: the line editor reads keys,
    // so it cannot be used, and a non-interactive console selects whole-line reading instead.

    private static TestConsole CreatePipedConsole()
    {
        var console = CreateConsole();
        console.Profile.Capabilities.Interactive = false;
        return console;
    }

    [Fact]
    public void PipedInput_RunsTheSessionAndStopsAtEndOfInput()
    {
        var console = CreatePipedConsole();
        var input = new StringReader(string.Join(Environment.NewLine,
            "#!syntax version 1.0",
            "lowclosure := 3",
            "lowclosure + 1"));

        var exitCode = new TestableInteractiveCommand(console, input)
            .Invoke(new InteractiveCommand.Settings { MuteWelcomeMessage = true });

        Assert.Equal(0, exitCode);
        Assert.Contains("Syntax version set to 1.0.", console.Output);
        // the keyword of a later version is usable as a variable, as the declared version has no such keyword
        Assert.Contains("lowclosure = 3", console.Output);
        Assert.Contains("4", console.Output);
        // reaching the end of the input ends the session, without needing !quit
        Assert.Contains("Bye.", console.Output);
    }

    [Fact]
    public void PipedInput_EchoesTheStatements()
    {
        // a pipe does not echo what was typed, so the session has to
        var console = CreatePipedConsole();
        var input = new StringReader("a := 5");

        new TestableInteractiveCommand(console, input)
            .Invoke(new InteractiveCommand.Settings { MuteWelcomeMessage = true });

        Assert.Contains("a := 5", console.Output);
    }

    [Fact]
    public void PipedInput_HonoursInteractiveCommands()
    {
        var console = CreatePipedConsole();
        var input = new StringReader(string.Join(Environment.NewLine, "a := 5", "!clear", "a"));

        new TestableInteractiveCommand(console, input)
            .Invoke(new InteractiveCommand.Settings { MuteWelcomeMessage = true });

        Assert.Contains("Session cleared.", console.Output);
    }

    [Fact]
    public void LineInputFalse_ForcesTheEditorOnANonInteractiveConsole()
    {
        // the explicit option overrides the detection, in both directions
        var console = CreatePipedConsole();
        console.Input.PushTypedLine("a := 7");
        console.Input.PushTypedLine("!quit");

        var exitCode = new TestableInteractiveCommand(console, new StringReader("a := 999"))
            .Invoke(new InteractiveCommand.Settings { MuteWelcomeMessage = true, LineInput = false });

        Assert.Equal(0, exitCode);
        // read through the editor, not from the piped source
        Assert.Contains("a = 7", console.Output);
        Assert.DoesNotContain("999", console.Output);
    }

    private static TestConsole CreateConsole()
    {
        var console = new TestConsole();
        console.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        console.Profile.Capabilities.Ansi = false;
        // these tests drive the line editor, as a user typing at a terminal would
        console.Profile.Capabilities.Interactive = true;
        console.Profile.Width = int.MaxValue;
        return console;
    }

    private static int RunInteractive(TestConsole console, string? exportRoot = null) =>
        new TestableInteractiveCommand(console).Invoke(new InteractiveCommand.Settings
        {
            MuteWelcomeMessage = true,
            ExportRoot = exportRoot
        });

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestableInteractiveCommand : InteractiveCommand
    {
        private readonly TextReader? _lineInput;

        public TestableInteractiveCommand(IAnsiConsole console, TextReader? lineInput = null) : base(console)
        {
            _lineInput = lineInput;
        }

        protected override TextReader LineInputSource => _lineInput ?? base.LineInputSource;

        public int Invoke(Settings settings) =>
            Execute(null!, settings, CancellationToken.None);
    }
}
