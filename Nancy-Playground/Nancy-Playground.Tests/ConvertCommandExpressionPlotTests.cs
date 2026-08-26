using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using static Unipi.Nancy.Playground.Cli.Tests.ConvertedProgram;

namespace Unipi.Nancy.Playground.Cli.Tests;

public class ConvertCommandExpressionPlotTests
{
    #pragma warning disable xUnit1051 // recommends xUnit cancellation token

    public static IEnumerable<string> TestDirs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "plot-expression-testcases");
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Missing testcases folder: {root}");

        return Directory
            .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsCaseDirectory)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> TestCases() =>
        TestDirs().Select(dir => new object[] { dir });

    private static bool IsCaseDirectory(string dir) =>
        File.Exists(Path.Combine(dir, "script.mppg"));

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterNancyExpressionsConversionProducesSamePlots(string caseDir)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var outputDir = Path.Combine(caseDir, "expression-plot-equivalence-test");
        var runOutputDir = Path.Combine(outputDir, "run");
        var convertOutputDir = Path.Combine(outputDir, "convert");
        Directory.CreateDirectory(runOutputDir);
        Directory.CreateDirectory(convertOutputDir);

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        var runConsole = CreateTestConsole();
        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        var runResult = runApp.Run([
            "run",
            scriptPath,
            "--no-welcome",
            "--plots-root", runOutputDir,
            "--no-gui"
        ]);

        Assert.Equal(0, runResult.ExitCode);

        var programPath = Path.Combine(convertOutputDir, "program.cs");
        var convertConsole = CreateTestConsole();
        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });

        var convertResult = convertApp.Run([
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite",
            "--use-expressions",
            "--no-welcome"
        ]);

        Assert.Equal(0, convertResult.ExitCode);
        Assert.True(File.Exists(programPath), $"Converted program not found at {programPath}.");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        BufferedCommandResult programResult;
        try
        {
            programResult = await CliWrap.Cli.Wrap("dotnet")
                .WithArguments(RunArguments(programPath))
                .WithWorkingDirectory(convertOutputDir)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Converted program did not exit within 60 seconds (case={caseDir}).");
        }

        Assert.Equal(0, programResult.ExitCode);

        var runPlotFiles = ExtractPlotPaths(runOutputDir).ToList();
        var convertPlotFiles = ExtractPlotPaths(convertOutputDir).ToList();

        Assert.Equal(runPlotFiles.Count, convertPlotFiles.Count);
        Assert.NotEmpty(runPlotFiles);

        for (var i = 0; i < runPlotFiles.Count; i++)
        {
            Assert.Equal(Path.GetFileName(runPlotFiles[i]), Path.GetFileName(convertPlotFiles[i]));
            Assert.Equal(
                await ComputeFileHashAsync(runPlotFiles[i]),
                await ComputeFileHashAsync(convertPlotFiles[i]));
        }
    }

    private static IEnumerable<string> ExtractPlotPaths(string dir) =>
        Directory
            .EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

    private static async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static TestConsole CreateTestConsole()
    {
        var console = new TestConsole();
        console.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        console.Profile.Capabilities.Ansi = false;
        console.Profile.Width = int.MaxValue;
        return console;
    }
}
