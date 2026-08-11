using System.Text;
using CliWrap;
using CliWrap.Buffered;

namespace Unipi.Nancy.Playground.Cli.Tests;

using CliMarker = Cli.Program;

/// <summary>
/// The encoding of the output of the CLI when it is redirected, i.e. read by another program.
/// </summary>
/// <remarks>
/// A process started with its output redirected gets a console of the machine's OEM code page, which
/// the expression output does not fit in: the CLI writes UTF-8 instead, so that what it prints does not
/// depend on the machine it runs on.
/// Covered here as well as by the golden cases, which would otherwise be the only place this shows up,
/// and only on a machine whose code page happens not to be UTF-8.
/// </remarks>
public class CliOutputEncodingTests
{
    #pragma warning disable xUnit1051 // recommends xUnit cancellation token

    private readonly ITestOutputHelper _testOutputHelper;

    public CliOutputEncodingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<object[]> NonAsciiOutputCases =>
        new List<(string script, string expected)>
        {
            // the unicode form of expressions, printed when a curve is assigned
            ("f := affine(1, 0)\nresult := upclosure(f)", "↑"),
            ("f := affine(1, 0)\nresult := floor(f)", "⌊"),
            ("f := affine(1, 0)\nresult := ceil(f)", "⌉"),
            ("f := affine(1, 0)\ng := affine(2, 0)\nresult := f * g", "⊗"),
            // and text of the script itself, echoed back
            ("// a comment with a ’ in it\nx := 1", "’"),
        }
        .Select(testCase => (object[])[testCase.script, testCase.expected]);

    [Theory]
    [MemberData(nameof(NonAsciiOutputCases))]
    public async Task RedirectedOutputIsUtf8(string script, string expected)
    {
        var cliDllPath = typeof(CliMarker).Assembly.Location;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"encoding-{Guid.NewGuid():N}.mppg");
        await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(false));

        try
        {
            var result = await CliWrap.Cli.Wrap("dotnet")
                .WithArguments([cliDllPath, "run", scriptPath, "--deterministic", "--no-welcome"])
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);

            _testOutputHelper.WriteLine(result.StandardOutput);

            Assert.Equal(0, result.ExitCode);
            // read as UTF-8: a mismatch means the CLI wrote in the code page of its console instead,
            // which turns these characters into a control character or an ASCII lookalike
            Assert.Contains(expected, result.StandardOutput);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }
}
