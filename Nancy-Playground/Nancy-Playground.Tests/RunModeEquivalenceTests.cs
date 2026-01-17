using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Xunit.Abstractions;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;

/// <summary>
/// Tests that MPPG scripts produce the same results when run in different modes:
/// PerStatement (immediate computation) vs ExpressionDriven (lazy computation).
/// 
/// Test cases use explicit print statements to capture results, ensuring output
/// matches regardless of computation strategy.
/// </summary>
public class RunModeEquivalenceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RunModeEquivalenceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<string> TestDirs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "run-mode-testcases");
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Missing testcases folder: {root}");

        var caseDirs = Directory
            .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsCaseDirectory)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);

        return caseDirs;
    }

    public static IEnumerable<object[]> TestCases() 
        => TestDirs().Select(dir => (object[])[dir]);

    private static bool IsCaseDirectory(string dir) =>
        File.Exists(Path.Combine(dir, "script.mppg"));

    private static string GetCurrentTfmFromPath(string assemblyPath)
    {
        // Typical path contains .../bin/Release/<tfm>/...
        var parts = assemblyPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var tfm = parts.FirstOrDefault(p => p.StartsWith("net", StringComparison.OrdinalIgnoreCase));
        return tfm ?? "unknown-tfm";
    }

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Trim();

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task PerStatementAndExpressionDrivenProduceSameResults(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "run-mode-comparison-test");
        Directory.CreateDirectory(outputDir);

        var scriptPath = Path.Combine(caseDir, "script.mppg");

        // Act: Run script in PerStatement mode
        string perStatementOutput;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult result;
            try
            {
                var args = new List<string>
                {
                    "run",
                    scriptPath,
                    "--output-mode", "ExplicitPrintsOnly",
                    "--run-mode", "PerStatement",
                    "--deterministic",
                    "--no-welcome"
                };

                var dotnetArgs = new List<string> { cliDllPath };
                dotnetArgs.AddRange(args);

                result = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"PerStatement run did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"per-statement.{tfm}.stdout.txt"), result.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"per-statement.{tfm}.stderr.txt"), result.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"per-statement.{tfm}.exitcode.txt"), result.ExitCode.ToString(), cts.Token);

            Assert.Equal(0, result.ExitCode);
            perStatementOutput = Normalize(result.StandardOutput);
        }

        // Act: Run script in ExpressionDriven mode
        string expressionDrivenOutput;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult result;
            try
            {
                var args = new List<string>
                {
                    "run",
                    scriptPath,
                    "--output-mode", "ExplicitPrintsOnly",
                    "--run-mode", "ExpressionDriven",
                    "--deterministic",
                    "--no-welcome"
                };

                var dotnetArgs = new List<string> { cliDllPath };
                dotnetArgs.AddRange(args);

                result = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"ExpressionDriven run did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"expression-driven.{tfm}.stdout.txt"), result.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"expression-driven.{tfm}.stderr.txt"), result.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"expression-driven.{tfm}.exitcode.txt"), result.ExitCode.ToString(), cts.Token);

            Assert.Equal(0, result.ExitCode);
            expressionDrivenOutput = Normalize(result.StandardOutput);
        }

        // Assert: Both modes should produce identical output
        _testOutputHelper.WriteLine("PerStatement output:");
        _testOutputHelper.WriteLine(perStatementOutput);
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("ExpressionDriven output:");
        _testOutputHelper.WriteLine(expressionDrivenOutput);

        Assert.Equal(perStatementOutput, expressionDrivenOutput);
    }
}
