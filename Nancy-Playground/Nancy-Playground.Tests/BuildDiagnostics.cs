using System.Text;
using CliWrap.Buffered;

namespace Unipi.Nancy.Playground.Cli.Tests;

/// <summary>
/// Reports why building a converted program failed.
/// </summary>
/// <remarks>
/// The build output is the only place where the cause is explained, e.g. a failed restore or a full build folder, hence it is part of the assertion message.
/// </remarks>
static class BuildDiagnostics
{
    /// <summary>
    /// The tail of the build output kept in the message, enough for the errors that close it.
    /// </summary>
    private const int MaxOutputChars = 4000;

    public static string BuildFailureMessage(BufferedCommandResult buildResult)
    {
        var message = new StringBuilder();
        message.AppendLine($"Building the converted program failed with exit code {buildResult.ExitCode}.");
        message.AppendLine($"Build folder: {BuildTempRoot.Path}");
        if (BuildTempRoot.InMemoryRejectedReason.Length > 0)
            message.AppendLine($"The in-memory build folder was not used, as it {BuildTempRoot.InMemoryRejectedReason}.");

        AppendTail(message, "stdout", buildResult.StandardOutput);
        AppendTail(message, "stderr", buildResult.StandardError);

        return message.ToString();
    }

    private static void AppendTail(StringBuilder message, string name, string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;

        var trimmed = output.Trim();
        if (trimmed.Length > MaxOutputChars)
            trimmed = $"...{trimmed[^MaxOutputChars..]}";

        message.AppendLine($"--- build {name}:");
        message.AppendLine(trimmed);
    }
}
