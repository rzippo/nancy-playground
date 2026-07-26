namespace Unipi.Nancy.Playground.Cli.Tests;

static class BuildTempRoot
{
    public static readonly string Path;

    static BuildTempRoot()
    {
        var env = Environment.GetEnvironmentVariable("NANCY_TEST_TEMP_ROOT");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env))
        {
            Path = System.IO.Path.Combine(env, "nancy-builds");
            return;
        }

        if (OperatingSystem.IsLinux() && Directory.Exists("/dev/shm"))
        {
            Path = "/dev/shm/nancy-builds";
            return;
        }

        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nancy-builds");
    }
}
