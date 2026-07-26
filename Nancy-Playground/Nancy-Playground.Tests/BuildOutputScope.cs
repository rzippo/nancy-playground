namespace Unipi.Nancy.Playground.Cli.Tests;

sealed class BuildOutputScope : IAsyncDisposable
{
    public string Path { get; }
    private readonly string _persistTo;
    private bool _failed;

    public BuildOutputScope(string persistTo)
    {
        _persistTo = persistTo;
        var unique = Guid.NewGuid().ToString("N")[..12];
        Path = System.IO.Path.Combine(BuildTempRoot.Path, unique);
        Directory.CreateDirectory(Path);
    }

    public void MarkFailed()
    {
        _failed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_failed)
        {
            try
            {
                CopyRecursive(Path, _persistTo);
            }
            catch
            {
            }
        }

        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
        catch
        {
        }

        await ValueTask.CompletedTask;
    }

    private static void CopyRecursive(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyRecursive(dir, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(dir)));
        }
    }
}
