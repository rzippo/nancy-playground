using System.Globalization;
using System.Text;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

public class CliCommandBranchCoverageTests
{
    [Fact]
    public void RunVersionPrintsVersionAndExitsZero()
    {
        var (exitCode, output) = InvokeRunCommand(new RunCommand.Settings
        {
            Version = true
        });

        Assert.Equal(0, exitCode);
        Assert.Contains("nancy-playground", output);
    }

    [Fact]
    public void RunWithoutInputReportsMissingFile()
    {
        var (exitCode, output) = InvokeRunCommand(new RunCommand.Settings
        {
            MuteWelcomeMessage = true
        });

        Assert.Equal(1, exitCode);
        Assert.Contains("No input file specified.", output);
    }

    [Fact]
    public void RunMissingFileReportsFileNotFound()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");

        var (exitCode, output) = RunCommand([
            "run",
            missingPath,
            "--deterministic",
            "--no-welcome"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("file not found", output);
    }

    [Fact]
    public void RunWelcomeAndVerboseBranchesArePrinted()
    {
        using var script = TemporaryScript.Create("1");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--verbose"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("nancy-playground", output);
        Assert.Contains("Parsing completed in", output);
    }

    [Fact]
    public void RunSavesPlotsNextToTheScriptByDefault()
    {
        using var scriptDirectory = TemporaryDirectory.Create();
        using var script = TemporaryScript.Create(
            """
            f := affine(1, 0)
            plotTikz(f, out = "chart.tex")
            """,
            scriptDirectory.Path);

        var (exitCode, _) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        // no plot root is given, so the default mode, ScriptDirectory, applies
        var codePath = Path.Combine(scriptDirectory.Path, "chart.tex");
        Assert.True(File.Exists(codePath), $"TikZ code not written to: {codePath}");
    }

    [Fact]
    public void RunAcceptsCurrentDirectoryPlotRootMode()
    {
        using var script = TemporaryScript.Create("1");

        var (exitCode, _) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--plots-root-mode", "CurrentDirectory"
        ]);

        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RunRejectsInvalidPlotRootConfiguration(bool suppliesPlotsRoot)
    {
        using var script = TemporaryScript.Create("1");
        using var root = TemporaryDirectory.Create();

        var settings = suppliesPlotsRoot
            ? new RunCommand.Settings
            {
                MppgFile = script.Path,
                Deterministic = true,
                MuteWelcomeMessage = true,
                PlotsRoot = root.Path,
                PlotsRootMode = PlotRootMode.ScriptDirectory
            }
            : new RunCommand.Settings
            {
                MppgFile = script.Path,
                Deterministic = true,
                MuteWelcomeMessage = true,
                PlotsRootMode = PlotRootMode.Custom
            };

        var ex = Assert.Throws<InvalidOperationException>(() => InvokeRunCommand(settings));
        Assert.Contains("--plots-root", ex.Message);
    }

    [Fact]
    public void RunMissingPlotsRootIsReportedBeforeParsing()
    {
        using var script = TemporaryScript.Create("1");
        var missingRoot = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--no-welcome",
            "--plots-root", missingRoot
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("directory not found", output);
        Assert.DoesNotContain("Parsing completed in", output);
    }

    [Fact]
    public void RunPlotTikzWithAbsoluteOutIgnoresThePlotsRoot()
    {
        using var root = TemporaryDirectory.Create();
        using var elsewhere = TemporaryDirectory.Create();
        var codePath = Path.Combine(elsewhere.Path, "chart.tex");

        using var script = TemporaryScript.Create(
            $"""
            f := affine(1, 0)
            plotTikz(f, out = "{codePath}")
            """);

        var (exitCode, _) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--plots-root", root.Path
        ]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(codePath), $"TikZ code not written to: {codePath}");
        Assert.Empty(Directory.GetFiles(root.Path));
    }

    [Fact]
    public void RunMppgClassicOutputModeExecutesPlainFormatter()
    {
        using var script = TemporaryScript.Create(
            """
            x := 1
            x
            """);

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--output-mode", "MppgClassic",
            "--run-mode", "PerStatement"
        ]);

        Assert.Equal(0, exitCode);
        // the formatter of this mode writes to the console it is given, so its output is captured
        Assert.Contains(">> x", output);
        Assert.Contains(">> 1", output);
    }

    [Fact]
    public void RunDeterministicPlotReportsPlotsDisabled()
    {
        using var script = TemporaryScript.Create(
            """
            f := affine(1, 0)
            plot(f, gui = "no")
            """);

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Plots disabled.", output);
    }

    [Fact]
    public void RunDeterministicPlotTikzPrintsTikzCode()
    {
        using var script = TemporaryScript.Create(
            """
            f := affine(1, 0)
            plotTikz(f)
            """);

        // TikZ plots are code, not images, hence they are not disabled by --deterministic
        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("\\begin{tikzpicture}", output);
        Assert.Contains("\\end{tikzpicture}", output);
    }

    [Fact]
    public void RunPlotTikzWithOutWritesFileWithTikzExtension()
    {
        using var script = TemporaryScript.Create(
            """
            f := affine(1, 0)
            plotTikz(f, out = "chart.png")
            """);
        using var root = TemporaryDirectory.Create();

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--plots-root", root.Path
        ]);

        Assert.Equal(0, exitCode);
        // the wrong extension is replaced, and the confirmation reports the file actually written
        var codePath = Path.Combine(root.Path, "chart.tikz");
        Assert.True(File.Exists(codePath));
        Assert.False(File.Exists(Path.Combine(root.Path, "chart.png")));
        Assert.Contains("chart.tikz", output);
        Assert.Contains("\\begin{tikzpicture}", File.ReadAllText(codePath));
    }

    [Fact]
    public void RunBannerSaysWhichPlotsThePlotsRootHolds()
    {
        using var script = TemporaryScript.Create("1");
        using var root = TemporaryDirectory.Create();

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--no-welcome",
            "--plots-root", root.Path
        ]);

        Assert.Equal(0, exitCode);
        // a plot without the out option is transient, and goes to the temp directory instead
        Assert.Contains("Plots with the out option will be saved in", output);
        Assert.Contains(root.Path, output);
    }

    [Fact]
    public void RunNoGuiWritesThePlotWithoutOpeningIt()
    {
        using var script = TemporaryScript.Create(
            """
            f := affine(1, 0)
            plot(f, out = "chart.png")
            """);
        using var root = TemporaryDirectory.Create();

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--no-welcome",
            "--plots-root", root.Path,
            "--no-gui"
        ]);

        Assert.Equal(0, exitCode);
        // the plot defaults to being shown, so the option of the command line is what skips it
        Assert.Contains("GUI disabled with --no-gui", output);
        Assert.True(File.Exists(Path.Combine(root.Path, "chart.png")));
    }

    [Fact]
    public void RunPlotTikzWithoutFunctionsReportsNothingToPlot()
    {
        using var script = TemporaryScript.Create(
            """
            plotTikz(xlim = [0, 10])
            """);

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("No functions to plot.", output);
    }

    [Fact]
    public void ConvertVersionPrintsVersionAndExitsZero()
    {
        var (exitCode, output) = InvokeConvertCommand(new ConvertCommand.Settings
        {
            Version = true
        });

        Assert.Equal(0, exitCode);
        Assert.Contains("nancy-playground", output);
    }

    [Fact]
    public void ConvertWithoutInputReportsMissingFile()
    {
        var (exitCode, output) = InvokeConvertCommand(new ConvertCommand.Settings
        {
            MuteWelcomeMessage = true
        });

        Assert.Equal(1, exitCode);
        Assert.Contains("No input file specified.", output);
    }

    [Fact]
    public void ConvertMissingFileReportsFileNotFound()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"nancy-playground-{Guid.NewGuid():N}.mppg");

        var (exitCode, output) = ConvertCommand([
            "convert",
            missingPath,
            "--no-welcome"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("file not found", output);
    }

    [Fact]
    public void ConvertUsesDefaultOutputPathWhenOutputFileIsOmitted()
    {
        using var dir = TemporaryDirectory.Create();
        var scriptPath = Path.Combine(dir.Path, "source.mppg");
        File.WriteAllText(scriptPath, "1", Encoding.UTF8);

        var (exitCode, _) = ConvertCommand([
            "convert",
            scriptPath,
            "--overwrite",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(dir.Path, "source.mppg.cs")));
    }

    [Fact]
    public void ConvertRecordsTheSourceNameByDefault()
    {
        using var dir = TemporaryDirectory.Create();
        var scriptPath = Path.Combine(dir.Path, "source.mppg");
        File.WriteAllText(scriptPath, "1", Encoding.UTF8);
        var outputPath = Path.Combine(dir.Path, "program.cs");

        var (exitCode, _) = ConvertCommand([
            "convert",
            scriptPath,
            "--output-file", outputPath,
            "--overwrite",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        var program = File.ReadAllText(outputPath);
        Assert.Contains("// Converted from source.mppg", program);
        // the local directory layout is not disclosed, so the program can be shared as it is
        Assert.DoesNotContain(dir.Path, program);
    }

    [Fact]
    public void ConvertRecordsTheFullSourcePathOnRequest()
    {
        using var dir = TemporaryDirectory.Create();
        var scriptPath = Path.Combine(dir.Path, "source.mppg");
        File.WriteAllText(scriptPath, "1", Encoding.UTF8);
        var outputPath = Path.Combine(dir.Path, "program.cs");

        var (exitCode, _) = ConvertCommand([
            "convert",
            scriptPath,
            "--output-file", outputPath,
            "--full-source-path",
            "--overwrite",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"// Converted from {scriptPath}", File.ReadAllText(outputPath));
    }

    [Fact]
    public void ConvertLegacyStringConversionWritesOutput()
    {
        using var dir = TemporaryDirectory.Create();
        var scriptPath = Path.Combine(dir.Path, "source.mppg");
        File.WriteAllText(
            scriptPath,
            """
            // heading
            x := 1
            x := x + 1
            """,
            Encoding.UTF8);
        var outputPath = Path.Combine(dir.Path, "program.cs");

        var (exitCode, _) = ConvertCommand([
            "convert",
            scriptPath,
            "--output-file", outputPath,
            "--legacy-string-conversion",
            "--overwrite",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputPath), $"Program not converted to: {outputPath}");
    }

    [Fact]
    public void ConvertRefusesExistingOutputWithoutOverwrite()
    {
        using var dir = TemporaryDirectory.Create();
        var scriptPath = Path.Combine(dir.Path, "source.mppg");
        var outputPath = Path.Combine(dir.Path, "program.cs");
        File.WriteAllText(scriptPath, "1", Encoding.UTF8);
        File.WriteAllText(outputPath, "// existing", Encoding.UTF8);

        var (exitCode, output) = ConvertCommand([
            "convert",
            scriptPath,
            "--output-file", outputPath,
            "--no-welcome"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("file already exists", output);
    }

    [Fact]
    public void ConvertInvalidSyntaxReportsCompileError()
    {
        using var script = TemporaryScript.Create("broken := (");
        using var dir = TemporaryDirectory.Create();

        var (exitCode, output) = ConvertCommand([
            "convert",
            script.Path,
            "--output-file", Path.Combine(dir.Path, "program.cs"),
            "--overwrite",
            "--no-welcome"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Cannot compile program", output);
    }

    [Fact]
    public void ConvertInvalidSyntaxShowsTheOffendingLine()
    {
        using var script = TemporaryScript.Create("f := bucket(2, 5)\ng := f + missing");
        using var dir = TemporaryDirectory.Create();

        var (exitCode, output) = ConvertCommand([
            "convert",
            script.Path,
            "--output-file", Path.Combine(dir.Path, "program.cs"),
            "--overwrite",
            "--no-welcome"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("line 2:9", output);
        Assert.Contains("g := f + missing", output);
    }

    /// <summary>
    /// The same --syntax-version/--syntax-version-forced pair as run, promoted onto
    /// CommonExecutionSettings: 'printExpression' is a 1.1 keyword, so convert fails to compile it
    /// under 1.0 and succeeds once a version that has it applies.
    /// </summary>
    [Fact]
    public void ConvertSyntaxVersionFillsInWhereTheFileDeclaresNone()
    {
        using var script = TemporaryScript.Create("a := 1\nprintExpression(a)");
        using var dir = TemporaryDirectory.Create();

        var (exitCode, output) = ConvertCommand([
            "convert",
            script.Path,
            "--output-file", Path.Combine(dir.Path, "program.cs"),
            "--overwrite",
            "--no-welcome",
            "--syntax-version", "1.0"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Cannot compile program", output);
    }

    [Fact]
    public void ConvertSyntaxVersionLosesToTheFilesOwnDirective()
    {
        using var script = TemporaryScript.Create("#!syntax version 1.1\na := 1\nprintExpression(a)");
        using var dir = TemporaryDirectory.Create();

        var (exitCode, output) = ConvertCommand([
            "convert",
            script.Path,
            "--output-file", Path.Combine(dir.Path, "program.cs"),
            "--overwrite",
            "--no-welcome",
            "--syntax-version", "1.0"
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Conversion complete.", output);
    }

    [Fact]
    public void ConvertSyntaxVersionForcedWinsOverTheFilesOwnDirective()
    {
        using var script = TemporaryScript.Create("#!syntax version 1.1\na := 1\nprintExpression(a)");
        using var dir = TemporaryDirectory.Create();

        var (exitCode, output) = ConvertCommand([
            "convert",
            script.Path,
            "--output-file", Path.Combine(dir.Path, "program.cs"),
            "--overwrite",
            "--no-welcome",
            "--syntax-version-forced", "1.0"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Cannot compile program", output);
    }

    [Theory]
    [InlineData("--syntax-version")]
    [InlineData("--syntax-version-forced")]
    public void ConvertMalformedSyntaxVersionReportsAnError(string option)
    {
        using var script = TemporaryScript.Create("a := 1");
        using var dir = TemporaryDirectory.Create();

        var (exitCode, output) = ConvertCommand([
            "convert",
            script.Path,
            "--output-file", Path.Combine(dir.Path, "program.cs"),
            "--overwrite",
            "--no-welcome",
            option, "not-a-version"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("'not-a-version' is not a valid syntax version", output);
    }

    /// <summary>
    /// --syntax-version only fills in where the file declares no version of its own: 'printExpression'
    /// is a 1.1 keyword, so under 1.0 it fails on the directiveless script and succeeds once the file's
    /// own 1.1 directive is left free to override the default.
    /// </summary>
    [Fact]
    public void RunSyntaxVersionFillsInWhereTheFileDeclaresNone()
    {
        using var script = TemporaryScript.Create("a := 1\nprintExpression(a)");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--syntax-version", "1.0"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("ERROR! Syntax errors, run aborted:", output);
    }

    [Fact]
    public void RunSyntaxVersionLosesToTheFilesOwnDirective()
    {
        using var script = TemporaryScript.Create("#!syntax version 1.1\na := 1\nprintExpression(a)");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--syntax-version", "1.0"
        ]);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("ERROR!", output);
    }

    [Fact]
    public void RunSyntaxVersionForcedWinsOverTheFilesOwnDirective()
    {
        using var script = TemporaryScript.Create("#!syntax version 1.1\na := 1\nprintExpression(a)");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--syntax-version-forced", "1.0"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("ERROR! Syntax errors, run aborted:", output);
    }

    /// <summary>
    /// Given both, --syntax-version-forced takes over outright rather than being combined or rejected
    /// as a conflict: --syntax-version has a non-null default ("latest"), so there is no way to tell
    /// "the user typed it" apart from "it was never touched".
    /// </summary>
    [Fact]
    public void RunSyntaxVersionForcedWinsOverPlainSyntaxVersionWhenBothAreGiven()
    {
        using var script = TemporaryScript.Create("a := 1\nprintExpression(a)");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--syntax-version", "1.1",
            "--syntax-version-forced", "1.0"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("ERROR! Syntax errors, run aborted:", output);
    }

    [Fact]
    public void RunSyntaxVersionDefaultsToLatest()
    {
        using var script = TemporaryScript.Create("a := 1\nprintExpression(a)");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome"
        ]);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("ERROR!", output);
    }

    [Theory]
    [InlineData("--syntax-version")]
    [InlineData("--syntax-version-forced")]
    public void RunMalformedSyntaxVersionReportsAnError(string option)
    {
        using var script = TemporaryScript.Create("a := 1");

        var (exitCode, output) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            option, "not-a-version"
        ]);

        Assert.Equal(1, exitCode);
        Assert.Contains("'not-a-version' is not a valid syntax version", output);
    }

    private static (int ExitCode, string Output) RunCommand(string[] args)
    {
        var console = CreateTestConsole();
        var app = new CommandAppTester(console: console);
        app.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        var result = app.Run(args);
        return (result.ExitCode, console.Output);
    }

    private static (int ExitCode, string Output) InvokeRunCommand(RunCommand.Settings settings)
    {
        var console = CreateTestConsole();
        var command = new TestableRunCommand(console);
        return (command.Invoke(settings), console.Output);
    }

    private static (int ExitCode, string Output) ConvertCommand(string[] args)
    {
        var console = CreateTestConsole();
        var app = new CommandAppTester(console: console);
        app.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });

        var result = app.Run(args);
        return (result.ExitCode, console.Output);
    }

    private static (int ExitCode, string Output) InvokeConvertCommand(ConvertCommand.Settings settings)
    {
        var console = CreateTestConsole();
        var command = new TestableConvertCommand(console);
        return (command.Invoke(settings), console.Output);
    }

    private static TestConsole CreateTestConsole()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var console = new TestConsole();
        console.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        console.Profile.Capabilities.Ansi = false;
        console.Profile.Width = int.MaxValue;
        return console;
    }

    private sealed class TestableRunCommand : RunCommand
    {
        public TestableRunCommand(TestConsole console) : base(console)
        {
        }

        public int Invoke(Settings settings) =>
            Execute(null!, settings, CancellationToken.None);
    }

    private sealed class TestableConvertCommand : ConvertCommand
    {
        public TestableConvertCommand(TestConsole console) : base(console)
        {
        }

        public int Invoke(Settings settings) =>
            Execute(null!, settings, CancellationToken.None);
    }

    private sealed class TemporaryScript : IDisposable
    {
        public string Path { get; }

        private TemporaryScript(string path)
        {
            Path = path;
        }

        public static TemporaryScript Create(string text, string? directory = null)
        {
            var path = System.IO.Path.Combine(
                directory ?? System.IO.Path.GetTempPath(),
                $"nancy-playground-{Guid.NewGuid():N}.mppg");
            File.WriteAllText(path, text, Encoding.UTF8);
            return new TemporaryScript(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; }

        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"nancy-playground-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
