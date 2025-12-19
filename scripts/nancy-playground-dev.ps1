Param(
    [switch]$DebugConfig = $false,
    [switch]$RunVerbose = $false
);

$projectPath = "!! REPLACE projectPath !!";

$configuration = $DebugConfig ? "Debug" : "Release";
$runVerbosity = $RunVerbose ? "detailed" : "quiet";
$tfm = "net10.0"

$scriptParameters = $MyInvocation.MyCommand.Parameters.Keys;
$programArgs = $args | Where-Object { -not ( $scriptParameters -contains $_ ) } | ForEach-Object { "`"$_`"" };
$joinedProgramArgs = $programArgs -join " ";

dotnet restore $projectPath --verbosity $runVerbosity --nologo;
dotnet build $projectPath --configuration $configuration --framework $tfm --verbosity $runVerbosity --no-restore --nologo;
Invoke-Expression "dotnet run --configuration $configuration --project $projectPath --framework $tfm --verbosity $runVerbosity --no-restore --no-build -- $joinedProgramArgs";
