using System.Diagnostics;

namespace Unipi.Nancy.Playground.Cli.Tests;

/// <summary>
/// The root folder where the converted programs are built.
/// </summary>
/// <remarks>
/// An in-memory folder is preferred, as it makes the builds faster, but it is usable only if it can host them.
/// It must have room for the build outputs, about 160 MB each, and allow loading their native dependencies, which a noexec mount does not.
/// Both are false for the default /dev/shm of a container, hence the choice is verified at runtime, falling back to an on-disk folder.
/// </remarks>
static class BuildTempRoot
{
    public static readonly string Path;

    /// <summary>
    /// The reason why the in-memory folder was not used, empty if it was.
    /// </summary>
    public static readonly string InMemoryRejectedReason = string.Empty;

    /// <summary>
    /// Room for the build outputs that run in parallel, each about 160 MB.
    /// </summary>
    private static readonly long RequiredFreeBytes =
        160L * 1024 * 1024 * Math.Clamp(Environment.ProcessorCount, 2, 8);

    static BuildTempRoot()
    {
        var onDisk = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nancy-builds");

        var configured = Environment.GetEnvironmentVariable("NANCY_TEST_TEMP_ROOT");
        if (!string.IsNullOrEmpty(configured) && Directory.Exists(configured))
        {
            Path = System.IO.Path.Combine(configured, "nancy-builds");
            return;
        }

        if (OperatingSystem.IsLinux() && Directory.Exists("/dev/shm"))
        {
            var inMemory = "/dev/shm/nancy-builds";
            InMemoryRejectedReason = DescribeUnsuitability(inMemory);
            if (InMemoryRejectedReason.Length == 0)
            {
                Path = inMemory;
                return;
            }
        }

        Path = onDisk;
    }

    /// <summary>
    /// Describes why the given folder cannot host the builds, or returns an empty string if it can.
    /// </summary>
    internal static string DescribeUnsuitability(string root)
    {
        try
        {
            Directory.CreateDirectory(root);
        }
        catch (Exception e)
        {
            return $"cannot be created ({e.Message})";
        }

        try
        {
            var freeBytes = new DriveInfo(root).AvailableFreeSpace;
            if (freeBytes < RequiredFreeBytes)
                return $"has {freeBytes / (1024 * 1024)} MB free, less than the {RequiredFreeBytes / (1024 * 1024)} MB needed";
        }
        catch (Exception e)
        {
            return $"free space cannot be read ({e.Message})";
        }

        return CanExecuteFrom(root)
            ? string.Empty
            : "does not allow executing its files, e.g. because it is mounted noexec, so native dependencies cannot be loaded";
    }

    /// <summary>
    /// True if a file in the given folder can be executed, which is also what loading a native library requires.
    /// </summary>
    private static bool CanExecuteFrom(string root)
    {
        var probePath = System.IO.Path.Combine(root, $"exec-probe-{Guid.NewGuid():N}.sh");
        try
        {
            File.WriteAllText(probePath, "#!/bin/sh\nexit 0\n");
            File.SetUnixFileMode(probePath, UnixFileMode.UserRead | UnixFileMode.UserExecute);

            using var probe = Process.Start(new ProcessStartInfo(probePath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (probe is null)
                return false;

            probe.WaitForExit();
            return probe.ExitCode == 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            try
            {
                File.Delete(probePath);
            }
            catch
            {
                // the probe file is left behind, which does not affect the builds
            }
        }
    }
}
