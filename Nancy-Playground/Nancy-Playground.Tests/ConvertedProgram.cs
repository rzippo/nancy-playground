namespace Unipi.Nancy.Playground.Cli.Tests;

/// <summary>
/// The command lines that build and run a converted program.
/// </summary>
/// <remarks>
/// A converted program pins its dependencies with <c>#:package</c>, which dotnet materializes as PackageReference items carrying a version.
/// Central package management, which the catalog above this tree enables, rejects those with NU1008, so the generated project opts out of it.
/// </remarks>
static class ConvertedProgram
{
    private const string OutsideCentralPackageManagement = "-p:ManagePackageVersionsCentrally=false";

    public static string[] BuildArguments(string programPath, string buildDir) =>
        ["build", programPath, "-o", buildDir, OutsideCentralPackageManagement];

    public static string[] RunArguments(string programPath) =>
        [programPath, OutsideCentralPackageManagement];
}
