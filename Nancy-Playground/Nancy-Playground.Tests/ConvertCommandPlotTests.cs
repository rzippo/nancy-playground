using System.Security.Cryptography;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Xunit.Abstractions;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;

/// <summary>
/// Tests that plot commands produce the same PNG images when running
/// the MPPG script and when running the converted C# program.
/// </summary>
public class ConvertCommandPlotTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConvertCommandPlotTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<string> TestDirs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "plot-testcases");
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
        // We'll grab the first segment that looks like netX.Y
        var parts = assemblyPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var tfm = parts.FirstOrDefault(p => p.StartsWith("net", StringComparison.OrdinalIgnoreCase));
        return tfm ?? "unknown-tfm";
    }

    /// <summary>
    /// Computes SHA256 hash of a file for comparison.
    /// This allows us to compare images deterministically.
    /// </summary>
    private static async Task<string> ComputeFileHashAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Extracts all plot output file paths from stdout.
    /// Plot commands print the full path to the generated image file.
    /// </summary>
    private static IEnumerable<string> ExtractPlotPaths(string stdout)
    {
        return stdout
            .Replace("\r\n", "\n")
            .Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line) && line.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Select(line => line.Trim());
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task ConvertCommandSamePlotImages(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "plot-comparison-test");
        Directory.CreateDirectory(outputDir);

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        
        // Create subdirectories for run and convert outputs to avoid conflicts
        var runOutputDir = Path.Combine(outputDir, "run");
        var convertOutputDir = Path.Combine(outputDir, "convert");
        Directory.CreateDirectory(runOutputDir);
        Directory.CreateDirectory(convertOutputDir);

        // Act: Run the MPPG script to generate plots
        string runPlotPaths;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                var runCommandArgs = new List<string> { "run", scriptPath, "--no-welcome" };
                var dotnetRunCommandArgs = new List<string> { cliDllPath };
                dotnetRunCommandArgs.AddRange(runCommandArgs);

                runCommandResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetRunCommandArgs)
                    .WithWorkingDirectory(runOutputDir)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Run command did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString(), cts.Token);

            Assert.Equal(0, runCommandResult.ExitCode);
            runPlotPaths = runCommandResult.StandardOutput;
        }

        // Act: Convert the MPPG script to C#
        var programPath = Path.Combine(convertOutputDir, "program.cs");
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                var convertCommandArgs = new List<string> { "convert", scriptPath, "--output-file", programPath, "--overwrite" };
                var dotnetConvertCommandArgs = new List<string> { cliDllPath };
                dotnetConvertCommandArgs.AddRange(convertCommandArgs);

                convertCommandResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetConvertCommandArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Convert command did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.StandardOutput, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError, cts.Token);
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString(), cts.Token);

            Assert.True(File.Exists(programPath), $"Converted program not found at {programPath}");
            Assert.Equal(0, convertCommandResult.ExitCode);
        }

        // Act: Run the converted C# program to generate plots
        string convertPlotPaths;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            BufferedCommandResult programResult;
            try
            {
                var dotnetProgramArgs = new List<string> { programPath };

                programResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments(dotnetProgramArgs)
                    .WithWorkingDirectory(convertOutputDir)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Program did not exit within 30 seconds (TFM={tfm}, case={caseDir}).");
            }

            Assert.Equal(0, programResult.ExitCode);
            convertPlotPaths = programResult.StandardOutput;
        }

        // Assert: Verify that plot files exist and have matching content
        var runPlotFiles = ExtractPlotPaths(runPlotPaths).ToList();
        var convertPlotFiles = ExtractPlotPaths(convertPlotPaths).ToList();

        _testOutputHelper.WriteLine($"Run plot files: {string.Join(", ", runPlotFiles)}");
        _testOutputHelper.WriteLine($"Convert plot files: {string.Join(", ", convertPlotFiles)}");

        // Both runs should produce the same number of plot files
        Assert.Equal(runPlotFiles.Count, convertPlotFiles.Count);

        // Compare each plot file by hash
        for (int i = 0; i < runPlotFiles.Count; i++)
        {
            var runPlotFile = runPlotFiles[i];
            var convertPlotFile = convertPlotFiles[i];

            // Extract just the filename for comparison (paths may differ)
            var runFileName = Path.GetFileName(runPlotFile);
            var convertFileName = Path.GetFileName(convertPlotFile);

            Assert.Equal(runFileName, convertFileName);

            // Get full paths
            var runFilePath = Path.Combine(runOutputDir, runFileName);
            var convertFilePath = Path.Combine(convertOutputDir, convertFileName);

            Assert.True(File.Exists(runFilePath), $"Run plot file not found: {runFilePath}");
            Assert.True(File.Exists(convertFilePath), $"Convert plot file not found: {convertFilePath}");

            // Compare file hashes
            var runHash = await ComputeFileHashAsync(runFilePath);
            var convertHash = await ComputeFileHashAsync(convertFilePath);

            _testOutputHelper.WriteLine($"Plot file: {runFileName}");
            _testOutputHelper.WriteLine($"  Run hash:     {runHash}");
            _testOutputHelper.WriteLine($"  Convert hash: {convertHash}");

            Assert.Equal(runHash, convertHash);
        }
    }
}
