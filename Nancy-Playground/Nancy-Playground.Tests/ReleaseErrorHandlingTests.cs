using System.Text;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Unipi.Nancy.Playground.Cli.Tests;

public class ReleaseErrorHandlingTests
{
    private const string RecoveredSyntaxErrorProgram = """
        a := 1
        1 = 2
        b := 2
        printExpression(b)
        """;

    [Fact]
    public void StopAbortsBeforeExecutingRecoveredSyntaxErrorStatements()
    {
        using var script = TemporaryScript.Create(RecoveredSyntaxErrorProgram);
        var (exitCode, output) = Run(script.Path, "Stop");

        Assert.Equal(1, exitCode);
        Assert.Contains("ERROR! Syntax errors, run aborted:", output);
        Assert.DoesNotContain("printExpression", output);
    }

    [Fact]
    public void ContinueFormatsRecoveredSyntaxErrorStatementsInRelease()
    {
#if DEBUG
        return;
#else
        using var script = TemporaryScript.Create(RecoveredSyntaxErrorProgram);
        var (exitCode, output) = Run(script.Path, "Continue");

        Assert.Equal(0, exitCode);
        Assert.Contains("WARNING! Syntax errors:", output);
        Assert.Contains("Syntax error", output);
        Assert.Contains("2", output);
#endif
    }

    private static (int ExitCode, string Output) Run(string path, string onError)
    {
        var console = new TestConsole();
        console.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        console.Profile.Capabilities.Ansi = false;
        console.Profile.Width = int.MaxValue;

        var app = new CommandAppTester(console: console);
        app.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        var result = app.Run([
            "run",
            path,
            "--deterministic",
            "--no-welcome",
            "--output-mode",
            "NancyNew",
            "--run-mode",
            "PerStatement",
            "--on-error",
            onError
        ]);

        return (result.ExitCode, console.Output);
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
}
