using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using static Unipi.Nancy.Playground.Cli.Tests.BuildDiagnostics;
using static Unipi.Nancy.Playground.Cli.Tests.ConvertedProgram;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;

[CollectionDefinition(ConvertCommandOutputCollection.Name, DisableParallelization = true)]
public sealed class ConvertCommandOutputCollection
{
    public const string Name = nameof(ConvertCommandOutputCollection);
}

[Collection(ConvertCommandOutputCollection.Name)]
public class ConvertCommandOutputTests
{
    #pragma warning disable xUnit1051 // recommends xUnit cancellation token

    private readonly ITestOutputHelper _testOutputHelper;

    public ConvertCommandOutputTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<string> TestDirs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "convert-testcases");
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

    public static IEnumerable<object[]> TestCasesByExpressionMode() =>
        TestDirs().SelectMany(static dir => new[]
        {
            new object[] { dir, false },
            new object[] { dir, true }
        });

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

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n")
         .Replace("-1/0", "-Infinity")
         .Replace("1/0", "+Infinity")
         .Trim();

    /// <summary>
    /// Returns the last non-empty line, if it exists.
    /// If none exists, returns null.
    /// </summary>
    /// <param name="stdout"></param>
    /// <returns></returns>
    private static string? LastNonEmptyLine(string stdout)
    {
        return stdout
           .Replace("\r\n", "\n")
           .Split('\n')
           .Select(l => l.TrimEnd())
           .LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
    }

    /// <summary>
    /// Compares user-visible textual output from the run command and converted program.
    /// This is intentionally broad, but frail for curve-valued outputs because Nancy and
    /// Nancy.Expressions may use different ToString() formats for equivalent curves.
    /// Prefer numeric evaluations or assertions in new fixtures unless formatting itself is under test.
    /// </summary>
    private static void AssertSameTextOutput(string expected, string actual)
    {
        if (expected == actual) return;
        
        var normExpected = Normalize(expected);
        var normActual = Normalize(actual);
        if (normExpected == normActual) return;

        // More aggressive normalization: ignore all whitespace and line endings
        string NoWs(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "");
        if (NoWs(normExpected) == NoWs(normActual))
            return;

        // If they are still different, we fall back to the standard assertion for a nice diff
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Builds and launches the actual app: first in run mode, then convert.
    /// Then builds and launches the converted script to test that the last results match, using step-by-step computations. 
    /// </summary>
    [Theory]
    [MemberData(nameof(TestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliSameLastResult(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        // Because this test project is multi-targeted, dotnet test runs it per TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "last-result-test", "cli");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--run-mode", "PerStatement",
            "--deterministic",
            "--no-welcome"
        ];

        // Act: run command, obtain the script output

        string runCommandFinalResult;
        int runTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTimeoutSeconds)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetRunCommandArgs = new List<string> { cliDllPath };
                dotnetRunCommandArgs.AddRange(runCommandArgs);

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
            runCommandFinalResult = LastNonEmptyLine(runCommandResult.StandardOutput) ?? 
                                    throw new InvalidOperationException("No result from the run command!");
        }

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite"
        ];

        // Act: convert command, obtain the C# program
        int convertTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertTimeoutSeconds)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetConvertCommandArgs = new List<string> { cliDllPath };
                dotnetConvertCommandArgs.AddRange(convertCommandArgs);

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

            Assert.True(File.Exists(programPath));
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
            // Arrange: run the converted program
            string programFinalResult;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programFinalResult = LastNonEmptyLine(programResult.StandardOutput) ?? 
                                        throw new InvalidOperationException("No result from the program!");
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandFinalResult, programFinalResult);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }

    /// <summary>
    /// Tests the run and convert commands via AppTesters.
    /// Then builds and launches the converted script to test that the last results match, using step-by-step computations. 
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterSameLastResult(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Arrange
        // we locate the CLI dll only for the TFM string
        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var tfm = GetCurrentTfmFromPath(cliDllPath);

        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        var outputDir = Path.Combine(caseDir, "last-result-test", "app-tester");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--run-mode", "PerStatement",
            "--deterministic",
            "--no-welcome"
        ];
        
        var runConsole = new TestConsole();
        runConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        runConsole.Profile.Capabilities.Ansi = false;
        runConsole.Profile.Width = int.MaxValue;
        
        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        // Act: run command, obtain the script output
        var runCommandResult = runApp.Run(runCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString());
        
        var runCommandFinalResult = LastNonEmptyLine(runCommandResult.Output) ?? 
                                throw new InvalidOperationException("No result from the run command!");

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite"
        ];

        var convertConsole = new TestConsole();
        convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        convertConsole.Profile.Capabilities.Ansi = false;
        convertConsole.Profile.Width = int.MaxValue;
        
        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });
        
        // Act: convert command, obtain the C# program
        var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString());

        Assert.True(File.Exists(programPath));
        Assert.Equal(0, convertCommandResult.ExitCode);

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
            // Arrange: run the converted program (from here on, identical to the CLI test)
            string programFinalResult;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programFinalResult = LastNonEmptyLine(programResult.StandardOutput) ?? 
                                        throw new InvalidOperationException("No result from the program!");
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandFinalResult, programFinalResult);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }
    
    /// <summary>
    /// Builds and launches the actual app: first in run mode, then convert.
    /// Then builds and launches the converted script to test that the last results match, using expression computations. 
    /// </summary>
    [Theory]
    [MemberData(nameof(TestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliSameLastResultExpressions(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        // Because this test project is multi-targeted, dotnet test runs it per TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "last-result-expressions-test", "cli");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--run-mode", "ExpressionsBased",
            "--deterministic",
            "--no-welcome"
        ];

        // Act: run command, obtain the script output

        string runCommandFinalResult;
        int runTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTimeoutSeconds)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetRunCommandArgs = new List<string> { cliDllPath };
                dotnetRunCommandArgs.AddRange(runCommandArgs);

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
            runCommandFinalResult = LastNonEmptyLine(runCommandResult.StandardOutput) ?? 
                                    throw new InvalidOperationException("No result from the run command!");
        }

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite",
            "--use-expressions"
        ];

        // Act: convert command, obtain the C# program
        int convertTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertTimeoutSeconds)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetConvertCommandArgs = new List<string> { cliDllPath };
                dotnetConvertCommandArgs.AddRange(convertCommandArgs);

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

            Assert.True(File.Exists(programPath));
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
            // Arrange: run the converted program
            string programFinalResult;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programFinalResult = LastNonEmptyLine(programResult.StandardOutput) ?? 
                                        throw new InvalidOperationException("No result from the program!");
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandFinalResult, programFinalResult);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }

    /// <summary>
    /// Tests the run and convert commands via AppTesters.
    /// Then builds and launches the converted script to test that the last results match, using expression computations. 
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterSameLastResultExpressions(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Arrange: locate the CLI dll built for *this* test run's TFM.
        // Because this test project is multi-targeted, dotnet test runs it per TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "last-result-expressions-test", "app-tester");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--run-mode", "ExpressionsBased",
            "--deterministic",
            "--no-welcome"
        ];

        var runConsole = new TestConsole();
        runConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        runConsole.Profile.Capabilities.Ansi = false;
        runConsole.Profile.Width = int.MaxValue;
        
        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });
        
        // Act: run command, obtain the script output
        var runCommandResult = runApp.Run(runCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString());
        
        var runCommandFinalResult = LastNonEmptyLine(runCommandResult.Output) ?? 
                                throw new InvalidOperationException("No result from the run command!");

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite",
            "--use-expressions"
        ];

        var convertConsole = new TestConsole();
        convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        convertConsole.Profile.Capabilities.Ansi = false;
        convertConsole.Profile.Width = int.MaxValue;
        
        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });
        
        // Act: convert command, obtain the C# program
        var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString());

        Assert.True(File.Exists(programPath));
        Assert.Equal(0, convertCommandResult.ExitCode);

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
            // Arrange: run the converted program (from here on, identical to the CLI test)
            string programFinalResult;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programFinalResult = LastNonEmptyLine(programResult.StandardOutput) ?? 
                                        throw new InvalidOperationException("No result from the program!");
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandFinalResult, programFinalResult);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AppTesterUseCodeTreesBuildsRunsAndProducesExpectedOutput(bool useNancyExpressions)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var outputDir = Path.Combine(Path.GetTempPath(), $"nancy-code-trees-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        try
        {
            var scriptPath = Path.Combine(outputDir, "script.mppg");
            await File.WriteAllTextAsync(
                scriptPath,
                """
                r := 1
                f := ratency(r, 2)
                x := r + 1
                x
                """);

            var programPath = Path.Combine(outputDir, "program.cs");
            List<string> convertCommandArgs =
            [
                "convert",
                scriptPath,
                "--output-file", programPath,
                "--overwrite"
            ];
            if (useNancyExpressions)
                convertCommandArgs.Add("--use-expressions");

            var convertConsole = new TestConsole();
            convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            convertConsole.Profile.Capabilities.Ansi = false;
            convertConsole.Profile.Width = int.MaxValue;

            var convertApp = new CommandAppTester(console: convertConsole);
            convertApp.Configure(config =>
            {
                config.AddCommand<ConvertCommand>("convert");
            });

            var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
            await File.WriteAllTextAsync(Path.Combine(outputDir, "convert.stdout.txt"), convertCommandResult.Output);

            Assert.True(File.Exists(programPath));
            Assert.Equal(0, convertCommandResult.ExitCode);

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
                var programResult = await CliWrap.Cli.Wrap("dotnet")
                    .WithArguments([dllPath])
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);

                await File.WriteAllTextAsync(Path.Combine(outputDir, "program.stdout.txt"), programResult.StandardOutput);
                await File.WriteAllTextAsync(Path.Combine(outputDir, "program.stderr.txt"), programResult.StandardError);

                Assert.Equal(0, programResult.ExitCode);
                Assert.Equal("2", LastNonEmptyLine(programResult.StandardOutput));
            }
            catch
            {
                buildScope.MarkFailed();
                throw;
            }
        }
        finally
        {
            try
            {
                Directory.Delete(outputDir, true);
            }
            catch
            {
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestCasesByExpressionMode))]
    public Task AppTesterSameLastResultLegacyStringConversion(string caseDir, bool useNancyExpressions) =>
        AppTesterSameOutputLegacyStringConversion(
            caseDir,
            useNancyExpressions,
            compareExplicitPrints: false);

    [Theory]
    [MemberData(nameof(TestCasesByExpressionMode))]
    public Task AppTesterSameExplicitPrintsLegacyStringConversion(string caseDir, bool useNancyExpressions) =>
        AppTesterSameOutputLegacyStringConversion(
            caseDir,
            useNancyExpressions,
            compareExplicitPrints: true);

    /// <summary>
    /// Builds and launches the actual app: first in run mode, then convert.
    /// Then builds and launches the converted script to test that all explicit prints match, using step-by-step computations. 
    /// </summary>
    [Theory]
    [MemberData(nameof(TestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliSameExplicitPrints(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        // Because this test project is multi-targeted, dotnet test runs it per TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "explicit-prints-test", "cli");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--output-mode", "ConvertReferencePrints",
            "--run-mode", "PerStatement",
            "--deterministic",
            "--no-welcome"
        ];

        // Act: run command, obtain the script output

        string runCommandExplicitPrints;
        int runTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTimeoutSeconds)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetRunCommandArgs = new List<string> { cliDllPath };
                dotnetRunCommandArgs.AddRange(runCommandArgs);

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
            runCommandExplicitPrints = Normalize(runCommandResult.StandardOutput);
        }

        // Arrange: convert the script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite"
        ];

        // Act: convert command, obtain the C# program
        int convertTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertTimeoutSeconds)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetConvertCommandArgs = new List<string> { cliDllPath };
                dotnetConvertCommandArgs.AddRange(convertCommandArgs);

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

            Assert.True(File.Exists(programPath));
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
            // Arrange: run the converted program
            string programExplicitPrints;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programExplicitPrints = Normalize(programResult.StandardOutput);
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandExplicitPrints, programExplicitPrints);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }

    /// <summary>
    /// Tests the run and convert commands via AppTesters.
    /// Then builds and launches the converted script to test that all explicit prints match, using step-by-step computations. 
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterSameExplicitPrints(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Arrange
        // we locate the CLI dll only for the TFM string
        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var tfm = GetCurrentTfmFromPath(cliDllPath);

        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        var outputDir = Path.Combine(caseDir, "explicit-prints-test", "app-tester");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--output-mode", "ConvertReferencePrints",
            "--run-mode", "PerStatement",
            "--deterministic",
            "--no-welcome"
        ];

        var runConsole = new TestConsole();
        runConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        runConsole.Profile.Capabilities.Ansi = false;
        runConsole.Profile.Width = int.MaxValue;
        
        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        // Act: run command, obtain the script output
        var runCommandResult = runApp.Run(runCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString());
        
        Assert.Equal(0, runCommandResult.ExitCode);
        var runCommandExplicitPrints = Normalize(runCommandResult.Output);

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite"
        ];

        var convertConsole = new TestConsole();
        convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        convertConsole.Profile.Capabilities.Ansi = false;
        convertConsole.Profile.Width = int.MaxValue;
        
        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });
        
        // Act: convert command, obtain the C# program
        var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString());

        Assert.True(File.Exists(programPath));
        Assert.Equal(0, convertCommandResult.ExitCode);

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
            // Arrange: run the converted program (from here on, identical to the CLI test)
            string programExplicitPrints;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programExplicitPrints = Normalize(programResult.StandardOutput);
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandExplicitPrints, programExplicitPrints);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }
    
    /// <summary>
    /// Builds and launches the actual app: first in run mode, then convert.
    /// Then builds and launches the converted script to test that all explicit prints match, using expression computations. 
    /// </summary>
    [Theory]
    [MemberData(nameof(TestCases))]
    [ExcludeFromCodeCoverage]
    public async Task CliSameExplicitPrintsExpressions(string caseDir)
    {
        // Arrange: locate the CLI dll built for *this* test run's TFM.
        // Because this test project is multi-targeted, dotnet test runs it per TFM.
        var cliDllPath = typeof(CliMarker).Assembly.Location;

        _testOutputHelper.WriteLine($"cliDllPath: {cliDllPath}");
        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        if (string.IsNullOrWhiteSpace(cliDllPath) || !File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI assembly not found at: {cliDllPath}");

        var tfm = GetCurrentTfmFromPath(cliDllPath);

        var outputDir = Path.Combine(caseDir, "explicit-prints-expressions-test", "cli");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--output-mode", "ConvertReferencePrints",
            "--run-mode", "ExpressionsBased",
            "--deterministic",
            "--no-welcome"
        ];

        // Act: run command, obtain the script output

        string runCommandExplicitPrints;
        int runTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTimeoutSeconds)))
        {
            BufferedCommandResult runCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetRunCommandArgs = new List<string> { cliDllPath };
                dotnetRunCommandArgs.AddRange(runCommandArgs);

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
            runCommandExplicitPrints = Normalize(runCommandResult.StandardOutput);
        }

        // Arrange: convert the script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite",
            "--use-expressions"
        ];

        // Act: convert command, obtain the C# program
        int convertTimeoutSeconds = 60;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertTimeoutSeconds)))
        {
            BufferedCommandResult convertCommandResult;
            try
            {
                // Run framework-dependent output as: dotnet <YourCli.dll> <args...>
                var dotnetConvertCommandArgs = new List<string> { cliDllPath };
                dotnetConvertCommandArgs.AddRange(convertCommandArgs);

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

            Assert.True(File.Exists(programPath));
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
            // Arrange: run the converted program
            string programExplicitPrints;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programExplicitPrints = Normalize(programResult.StandardOutput);
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandExplicitPrints, programExplicitPrints);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }
    
    /// <summary>
    /// Tests the run and convert commands via AppTesters.
    /// Then builds and launches the converted script to test that all explicit prints match, using expression computations. 
    /// </summary>
    /// <remarks>
    /// Provides a debug path, and test coverage metrics.
    /// </remarks>
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task AppTesterSameExplicitPrintsExpressions(string caseDir)
    {
        // must be setup here, since AppTesters inherit it
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Arrange
        // we locate the CLI dll only for the TFM string
        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var tfm = GetCurrentTfmFromPath(cliDllPath);

        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        var outputDir = Path.Combine(caseDir, "explicit-prints-expressions-test", "app-tester");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs = [
            "run",
            scriptPath,
            "--output-mode", "ConvertReferencePrints",
            "--run-mode", "ExpressionsBased",
            "--deterministic",
            "--no-welcome"
        ];

        var runConsole = new TestConsole();
        runConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        runConsole.Profile.Capabilities.Ansi = false;
        runConsole.Profile.Width = int.MaxValue;
        
        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        // Act: run command, obtain the script output
        var runCommandResult = runApp.Run(runCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stderr.txt"), runCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString());
        
        Assert.Equal(0, runCommandResult.ExitCode);
        var runCommandExplicitPrints = Normalize(runCommandResult.Output);

        // Arrange: convert the MPPG script to a C# file-based app
        var programPath = Path.Combine(outputDir, "program.cs"); 
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite",
            "--use-expressions"
        ];

        var convertConsole = new TestConsole();
        convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        convertConsole.Profile.Capabilities.Ansi = false;
        convertConsole.Profile.Width = int.MaxValue;
        
        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });
        
        // Act: convert command, obtain the C# program
        var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
        
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.Output);
        // await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stderr.txt"), convertCommandResult.StandardError);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString());

        Assert.True(File.Exists(programPath));
        Assert.Equal(0, convertCommandResult.ExitCode);

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
            // Arrange: run the converted program (from here on, identical to the CLI test)
            string programExplicitPrints;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    var dotnetProgramArgs = new List<string> { dllPath };

                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments(dotnetProgramArgs)
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
                programExplicitPrints = Normalize(programResult.StandardOutput);
            }

            // Finally: check that both results are the same
            AssertSameTextOutput(runCommandExplicitPrints, programExplicitPrints);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }

    private async Task AppTesterSameOutputLegacyStringConversion(
        string caseDir,
        bool useNancyExpressions,
        bool compareExplicitPrints)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var tfm = GetCurrentTfmFromPath(cliDllPath);
        var modeName = useNancyExpressions ? "expressions" : "direct";
        var assertionName = compareExplicitPrints ? "explicit-prints" : "last-result";

        _testOutputHelper.WriteLine($"caseDir: {Path.GetFullPath(caseDir)}");

        var outputDir = Path.Combine(caseDir, $"{assertionName}-legacy-test", modeName, "app-tester");
        Directory.CreateDirectory(outputDir);
        _testOutputHelper.WriteLine($"outputDir: {Path.GetFullPath(outputDir)}");

        var scriptPath = Path.Combine(caseDir, "script.mppg");
        List<string> runCommandArgs =
        [
            "run",
            scriptPath
        ];
        if (compareExplicitPrints)
            runCommandArgs.AddRange(["--output-mode", "ConvertReferencePrints"]);
        runCommandArgs.AddRange([
            "--run-mode", useNancyExpressions ? "ExpressionsBased" : "PerStatement",
            "--deterministic",
            "--no-welcome"
        ]);

        var runConsole = new TestConsole();
        runConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        runConsole.Profile.Capabilities.Ansi = false;
        runConsole.Profile.Width = int.MaxValue;

        var runApp = new CommandAppTester(console: runConsole);
        runApp.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        var runCommandResult = runApp.Run(runCommandArgs.ToArray());
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.stdout.txt"), runCommandResult.Output);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"run.{tfm}.exitcode.txt"), runCommandResult.ExitCode.ToString());

        Assert.Equal(0, runCommandResult.ExitCode);
        var runReferenceOutput = compareExplicitPrints
            ? Normalize(runCommandResult.Output)
            : LastNonEmptyLine(runCommandResult.Output) ??
                throw new InvalidOperationException("No result from the run command!");

        var programPath = Path.Combine(outputDir, "program.cs");
        List<string> convertCommandArgs =
        [
            "convert",
            scriptPath,
            "--output-file", programPath,
            "--overwrite",
            "--legacy-string-conversion"
        ];
        if (useNancyExpressions)
            convertCommandArgs.Add("--use-expressions");

        var convertConsole = new TestConsole();
        convertConsole.Profile.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        convertConsole.Profile.Capabilities.Ansi = false;
        convertConsole.Profile.Width = int.MaxValue;

        var convertApp = new CommandAppTester(console: convertConsole);
        convertApp.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
        });

        var convertCommandResult = convertApp.Run(convertCommandArgs.ToArray());
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.stdout.txt"), convertCommandResult.Output);
        await File.WriteAllTextAsync(Path.Combine(outputDir, $"convert.{tfm}.exitcode.txt"), convertCommandResult.ExitCode.ToString());

        Assert.True(File.Exists(programPath));
        Assert.Equal(0, convertCommandResult.ExitCode);

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
            string programOutput;
            int convertedProgramTimeoutSeconds = 60;
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(convertedProgramTimeoutSeconds)))
            {
                BufferedCommandResult programResult;
                try
                {
                    programResult = await CliWrap.Cli.Wrap("dotnet")
                        .WithArguments([dllPath])
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
                programOutput = compareExplicitPrints
                    ? Normalize(programResult.StandardOutput)
                    : LastNonEmptyLine(programResult.StandardOutput) ??
                        throw new InvalidOperationException("No result from the program!");
            }

            AssertSameTextOutput(runReferenceOutput, programOutput);
        }
        catch
        {
            buildScope.MarkFailed();
            throw;
        }
    }
}
