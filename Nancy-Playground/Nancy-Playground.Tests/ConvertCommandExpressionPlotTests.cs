using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using static Unipi.Nancy.Playground.Cli.Tests.BuildDiagnostics;
using static Unipi.Nancy.Playground.Cli.Tests.ConvertedProgram;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;

/// <summary>
/// Tests that plot commands produce the same PNG images when running
/// the MPPG script and when running the converted C# program built from it,
/// using the expressions-based evaluation.
/// </summary>
public class ConvertCommandExpressionPlotTests
{
    #pragma warning disable xUnit1051 // recommends xUnit cancellation token

    private readonly ITestOutputHelper _testOutputHelper;

    public ConvertCommandExpressionPlotTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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

    private static string GetCurrentTfmFromPath(string assemblyPath)
    {
        // Typical path contains .../bin/Release/<tfm>/...
        // We'll grab the first segment that looks like netX.Y
        var parts = assemblyPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var tfm = parts.FirstOrDefault(p => p.StartsWith("net", StringComparison.OrdinalIgnoreCase));
        return tfm ?? "unknown-tfm";
    }

    /// <summary>
    /// Builds and launches the actual app: first in run mode, then convert.
    /// Then builds and launches the converted script to test that the same plots are produced.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliNancyExpressionsConversionProducesSamePlots(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "expression-plot-equivalence-test", "cli");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var runOutputDir = Path.Combine(outputDir, "run");
        var convertOutputDir = Path.Combine(outputDir, "convert");
        Directory.CreateDirectory(runOutputDir);
        Directory.CreateDirectory(convertOutputDir);

        var scriptPath = Path.Combine(caseDir, "script.mppg");

        // Act: run command, obtain the script plots
        int runTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTimeoutSeconds)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                var dotnetRunCommandArgs = new List<string>
                {
                    cliDllPath,
                    "run",
                    scriptPath,
                    "--no-welcome",
                    "--plots-root", runOutputDir,
                    "--no-gui"
                };

                runCommandResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetRunCommandArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"CLI did not exit within {runTimeoutSeconds} seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString(), cts.Token);

            Assert.Equal(0, runCommandResult.ExitCode);
        }

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(convertOutputDir, "program.cs");
        int convertTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertTimeoutSeconds)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                var dotnetConvertCommandArgs = new List<string>
                {
                    cliDllPath,
                    "convert",
                    scriptPath,
                    "--output-file", programPath,
                    "--overwrite",
                    "--use-expressions",
                    "--no-welcome"
                };

                convertCommandResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetConvertCommandArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"CLI did not exit within {convertTimeoutSeconds} seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString(), cts.Token);

            Assert.True(File.Exists(programPath), $"Converted program not found at {programPath}");
            Assert.Equal(0, convertCommandResult.ExitCode);
        }

        // Build the converted program (no timeout - handles NuGet restore)
        var buildPersistPath = Path.Combine(outputDir, "build-output");
        await using var buildScope = new BuildOutputScope(buildPersistPath);
        var buildDir = buildScope.Path;
        var buildResult = await CliWrap.Cli.Wrap("dotnet")
            .WithArguments(BuildArguments(programPath, buildDir))
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);
        Assert.True(
            buildResult.ExitCode == 0,
            BuildFailureMessage(buildResult));
        var dllPath = Path.Combine(buildDir, $"{Path.GetFileNameWithoutExtension(programPath)}.dll");
        Assert.True(File.Exists(dllPath), $"Built assembly not found at: {dllPath}");

        try
        {
            // Act: run the converted program, the only part kept on a timeout
            BufferedCommandResult programResult;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                try
                {
                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments([dllPath])
                        .WithWorkingDirectory(convertOutputDir)
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Program run did not exit within {convertedProgramTimeoutSeconds} seconds (TFM={tfm}, case={caseDir}).");
                }

                await File.WriteAllTextAsync(Path.Combine(outputDir, $"program.{tfm}.stdout.txt"), programResult.StandardOutput, cts.Token);
                await File.WriteAllTextAsync(Path.Combine(outputDir, $"program.{tfm}.stderr.txt"), programResult.StandardError, cts.Token);
                await File.WriteAllTextAsync(Path.Combine(outputDir, $"program.{tfm}.exitcode.txt"), programResult.ExitCode.ToString(), cts.Token);

                Assert.Equal(0, programResult.ExitCode);
            }

            // Assert: verify that plot files exist and have matching content
            var runPlotFiles = ExtractPlotPaths(runOutputDir).ToList();
            var convertPlotFiles = ExtractPlotPaths(convertOutputDir).ToList();

            _testOutputHelper.WriteLine($"Run plot files: [ {string.Join(", ", runPlotFiles)} ]");
            _testOutputHelper.WriteLine($"Convert plot files: [ {string.Join(", ", convertPlotFiles)} ]");

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
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }

    /// <summary>
    /// Tests the run and convert commands via AppTesters.
    /// Then builds and launches the converted script to test that the same plots are produced.
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterNancyExpressionsConversionProducesSamePlots(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // we locate the CLI dll only for the TFM string
        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var tfm = GetCurrentTfmFromPath(cliDllPath);

        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        var outputDir = Path.Combine(caseDir, "expression-plot-equivalence-test", "app-tester");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

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

        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runResult.Output);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runResult.ExitCode.ToString());

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

        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertResult.Output);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertResult.ExitCode.ToString());

        Assert.Equal(0, convertResult.ExitCode);
        Assert.True(File.Exists(programPath), $"Converted program not found at {programPath}.");

        // Build the converted program (no timeout - handles NuGet restore)
        var buildPersistPath = Path.Combine(outputDir, "build-output");
        await using var buildScope = new BuildOutputScope(buildPersistPath);
        var buildDir = buildScope.Path;
        var buildResult = await CliWrap.Cli.Wrap("dotnet")
            .WithArguments(BuildArguments(programPath, buildDir))
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);
        Assert.True(
            buildResult.ExitCode == 0,
            BuildFailureMessage(buildResult));
        var dllPath = Path.Combine(buildDir, $"{Path.GetFileNameWithoutExtension(programPath)}.dll");
        Assert.True(File.Exists(dllPath), $"Built assembly not found at: {dllPath}");

        try
        {
            // Act: run the converted program, the only part kept on a timeout
            BufferedCommandResult programResult;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                programResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments([dllPath])
                    .WithWorkingDirectory(convertOutputDir)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Converted program did not exit within 60 seconds (case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"program.{tfm}.stdout.txt"), programResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"program.{tfm}.stderr.txt"), programResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"program.{tfm}.exitcode.txt"), programResult.ExitCode.ToString(), cts.Token);

            Assert.Equal(0, programResult.ExitCode);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }

        var runPlotFiles = ExtractPlotPaths(runOutputDir).ToList();
        var convertPlotFiles = ExtractPlotPaths(convertOutputDir).ToList();

        _testOutputHelper.WriteLine($"Run plot files: [ {string.Join(", ", runPlotFiles)} ]");
        _testOutputHelper.WriteLine($"Convert plot files: [ {string.Join(", ", convertPlotFiles)} ]");

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
