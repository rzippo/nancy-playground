namespace Unipi.Nancy.Playground.Cli.Tests;

/// <summary>
/// Tests the choice of the folder where converted programs are built.
/// </summary>
public class BuildTempRootTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public BuildTempRootTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// Fails early, and with the reason, when no folder can host the builds:
    /// otherwise every test that builds a converted program fails with an unexplained exit code.
    /// </summary>
    [Fact]
    public void ChosenBuildRoot_CanHostBuilds()
    {
        _testOutputHelper.WriteLine($"build folder: {BuildTempRoot.Path}");
        if (BuildTempRoot.InMemoryRejectedReason.Length > 0)
            _testOutputHelper.WriteLine($"in-memory folder not used, as it {BuildTempRoot.InMemoryRejectedReason}");

        var unsuitability = BuildTempRoot.DescribeUnsuitability(BuildTempRoot.Path);

        Assert.True(
            unsuitability.Length == 0,
            $"The build folder {BuildTempRoot.Path} {unsuitability}. Set NANCY_TEST_TEMP_ROOT to a folder that can host the builds.");
    }

    [Fact]
    public void UnsuitableBuildRoot_IsDescribed()
    {
        // a folder that does not exist, and cannot be created, stands for any folder that cannot host the builds
        var unusable = Path.Combine(Path.GetTempPath(), $"nancy-probe-{Guid.NewGuid():N}", "\0invalid");

        var unsuitability = BuildTempRoot.DescribeUnsuitability(unusable);

        Assert.NotEmpty(unsuitability);
    }
}
