#!/bin/pwsh

Param(
    [switch]$Dev = $false,
    [switch]$DebugConfig = $false
);

$ErrorActionPreference = 'Stop'

if($IsWindows)
{
    $runConfiguration = $DebugConfig ? "Debug" : "Release";

    $projectName = "Nancy-Playground";
    $projectNameLc = $projectName.ToLowerInvariant();
    $projectRootPath = "./Nancy-Playground/$projectName";
    $projectPath = "$projectRootPath/$projectName.csproj";
    $scriptsFolder = "./scripts";

    $installFolder = "$env:LOCALAPPDATA/Programs/$projectNameLc";

    if($Dev)
    {
        # Create install folder if not exists, else clear its contents
        if( -not (Test-Path $installFolder) ) {
            New-Item $installFolder -ItemType Directory
        }
        else {
            Remove-Item -Recurse "$installFolder/*";
        }

        # Configure and copy ps script to install folder
        $fullProjectPath = Resolve-Path $projectPath;
        $psContent = Get-Content -Raw -Path "$scriptsFolder/$projectNameLc.ps1";
        $psContent = $psContent -replace "!! REPLACE projectPath !!",$fullProjectPath;
        
        $psContent | Out-File "$installFolder/$projectNameLc.ps1";

        # Copy bat script to install folder
        Copy-Item -Path "$scriptsFolder/$projectNameLc.bat" -Destination "$installFolder/$projectNameLc.bat";

        # If install folder is not in path, add it
        $pathVariable = [System.Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User);
        if(-not (($pathVariable -split ";") -contains $installFolder)) {
            $pathVariable += ";$installFolder";
            [System.Environment]::SetEnvironmentVariable("Path", $pathVariable, [EnvironmentVariableTarget]::User)
            $env:Path = $pathVariable;
        }

        Write-Host "Done installing: dev mode."
    }
    else
    {
        # Build latest code
        dotnet restore --no-http-cache $projectPath;
        dotnet publish --configuration $runConfiguration $projectPath;

        # Create install folder if not exists, else clear its contents
        if( -not (Test-Path $installFolder) ) {
            New-Item $installFolder -ItemType Directory -Force
        }
        else {
            Remove-Item -Recurse "$installFolder/*"
        }

        # Copy build to install folder
        $publishDir = "$projectRootPath/bin/$runConfiguration/net9.0/publish";
        Copy-Item -Recurse -Path "$publishDir/*"  -Destination $installFolder;

        # If install folder is not in path, add it
        $pathVariable = [System.Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User);
        if(-not (($pathVariable -split ";") -contains $installFolder)) {
            $pathVariable += ";$installFolder";
            [System.Environment]::SetEnvironmentVariable("Path", $pathVariable, [EnvironmentVariableTarget]::User)
            $env:Path = $pathVariable;
        }

        Write-Host "Done installing: compiled mode."
    }
}
elseif($IsLinux) 
{
    $runConfiguration = $DebugConfig ? "Debug" : "Release";

    $projectName = "Nancy-Playground";
    $projectNameLc = $projectName.ToLowerInvariant();
    $projectRootPath = "./Nancy-Playground/$projectName";
    $projectPath = "$projectRootPath/$projectName.csproj";
    $scriptsFolder = "./scripts";

    $installFolder = "/opt/$projectNameLc";

    if($Dev)
    {
        # Create install folder if not exists, else clear its contents
        if( -not (Test-Path $installFolder) ) {
            New-Item $installFolder -ItemType Directory
        }
        else {
            Remove-Item -Recurse "$installFolder/*";
        }

        # Make sure the install folder can be modified
        chmod -R a+wr $installFolder;

        # Configure and copy ps script to install folder
        $fullProjectPath = Resolve-Path $projectPath;
        $psContent = Get-Content -Raw -Path "$scriptsFolder/$projectNameLc.ps1";
        $psContent = $psContent -replace "!! REPLACE projectPath !!",$fullProjectPath;
        
        $psContent | Out-File "$installFolder/$projectNameLc.ps1";

        # Copy sh script to install folder
        Copy-Item -Path "$scriptsFolder/$projectNameLc.sh" -Destination "$installFolder/$projectNameLc";

        # If install folder is not in path, add it
        $profileScript = "/etc/profile.d/$projectNameLc.sh";
        if(-not (Test-Path $profileScript )) {
            "#!/bin/sh" | Out-File $profileScript;
            "export PATH=`"`$PATH:$installFolder`"" | Out-File $profileScript -Append;
            chmod a+x $profileScript;
        }

        Write-Host "Done installing: dev mode."
    }
    else
    {
        # Build latest code
        dotnet publish -c $runConfiguration $projectPath;

        # Create install folder if not exists, else clear its contents
        if( -not (Test-Path $installFolder) ) {
            New-Item $installFolder -ItemType Directory
        }
        else {
            Remove-Item -Recurse "$installFolder/*";
        }

        # Make sure the install folder can be modified
        chmod -R a+wr $installFolder;

        # Copy build to install folder
        $publishDir = "$projectRootPath/bin/$runConfiguration/net9.0/publish";
        Copy-Item -Recurse -Force -Path "$publishDir/*" -Destination $installFolder;

        # If install folder is not in path, add it
        $profileScript = "/etc/profile.d/$projectName.sh";
        if(-not (Test-Path $profileScript )) {
            "#!/bin/sh" | Out-File $profileScript;
            "export PATH=`"`$PATH:$installFolder`"" | Out-File $profileScript -Append;
            chmod a+x $profileScript;
        }

        Write-Host "Done installing: compiled mode."
    }
}
else
{
    Write-Host "This OS is not supported by this install script";
    exit;
}