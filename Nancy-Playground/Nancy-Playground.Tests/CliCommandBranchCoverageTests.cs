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
    public void RunMppgClassicOutputModeExecutesPlainFormatter()
    {
        using var script = TemporaryScript.Create(
            """
            x := 1
            x
            """);

        var (exitCode, _) = RunCommand([
            "run",
            script.Path,
            "--deterministic",
            "--no-welcome",
            "--output-mode", "MppgClassic",
            "--run-mode", "PerStatement"
        ]);

        Assert.Equal(0, exitCode);
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

        public static TemporaryScript Create(string text)
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
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
