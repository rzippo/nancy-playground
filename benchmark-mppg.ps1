#!/bin/pwsh
param(
    [Parameter(Mandatory=$true)]
    [string]$MppgScriptPath
)

$ErrorActionPreference = "Stop"

$benchmarkDate = Get-Date;

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir "Nancy-Playground"
$workingDir = Join-Path $scriptDir "benchmark-temp"
$convertedDir = Join-Path $workingDir "converted"

$MppgScriptPath = [System.IO.Path]::GetFullPath($MppgScriptPath)

New-Item -ItemType Directory -Path $workingDir -Force | Out-Null
New-Item -ItemType Directory -Path $convertedDir -Force | Out-Null

Write-Host "Working directory: $workingDir" -ForegroundColor Cyan

$cliProject = Join-Path $projectDir "Nancy-Playground\Nancy-Playground.csproj"
$mappingProject = Join-Path $projectDir "MppgParser\MppgParser.csproj"

$cliDllPath = Join-Path $projectDir "Nancy-Playground\bin\Release\net10.0\Unipi.Nancy.Playground.Cli.dll"
$mappingDllPath = Join-Path $projectDir "MppgParser\bin\Release\net10.0\Unipi.Nancy.Playground.MppgParser.dll"

Write-Host "`n=== Step 1: Building Nancy-Playground net10.0 Release ===" -ForegroundColor Yellow
dotnet build $cliProject -c Release -f net10.0 --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "CLI build failed" }
Write-Host "CLI build complete" -ForegroundColor Green

function Measure-Benchmark {
    param(
        [string]$ExePath,
        [string]$WorkingDir,
        [string[]]$Arguments,
        [int]$Iterations = 3,
        [int]$MemorySamplingInterval = 15
    )

    $times = @()
    $memories = @()

    $isVerbose = $VerbosePreference -eq "Continue";
    
    $dotnetArguments = "$ExePath $($Arguments -join ' ')"
    for ($i = 0; $i -lt $Iterations; $i++) {
        Write-Verbose "Iteration $($i + 1) of $Iterations`: $dotnetArguments"
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "dotnet"
        $psi.Arguments = $dotnetArguments
        $psi.WorkingDirectory = $WorkingDir
        $psi.UseShellExecute = $false
        if($isVerbose){
            $psi.RedirectStandardOutput = $false
            $psi.RedirectStandardError = $false
        }
        else {
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
        }

        $proc = [System.Diagnostics.Process]::Start($psi)
        if(-not $isVerbose) {
            # Need to read output asynchronously to prevent deadlocks if the process generates a lot of output
            $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
            $stderrTask = $proc.StandardError.ReadToEndAsync()
        }
        
        [int64]$maxMemory = 0
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        do {
            $maxMemory = [math]::Max([int64]$maxMemory, [int64]$proc.PeakWorkingSet64)
            $exited = $proc.WaitForExit($MemorySamplingInterval)
        } while (!$exited -and !$proc.HasExited)
        $stopwatch.Stop()

        $times += $stopwatch.ElapsedMilliseconds
        $memories += $maxMemory

        Start-Sleep -Milliseconds 100
    }

    $avgTime = ($times | Measure-Object -Average).Average
    $avgMem = ($memories | Measure-Object -Average).Average / 1MB

    return @{
        AvgTimeMs = [math]::Round($avgTime, 2)
        AvgMemoryMB = [math]::Round($avgMem, 2)
    }
}

$scriptContent = Get-Content $MppgScriptPath -Raw
$scriptName = [System.IO.Path]::GetFileName($MppgScriptPath)

Write-Host "`n=== Step 2: Benchmarking Nancy-Playground CLI ===" -ForegroundColor Yellow

$runResult = Measure-Benchmark -ExePath $cliDllPath -WorkingDir $workingDir -Arguments @("run", $MppgScriptPath) -Iterations 3

Write-Host "  Avg Time: $($runResult.AvgTimeMs) ms" -ForegroundColor Cyan
Write-Host "  Avg Memory: $($runResult.AvgMemoryMB) MB" -ForegroundColor Cyan

$cliTime = $runResult.AvgTimeMs
$cliMem = $runResult.AvgMemoryMB

Write-Host "`n=== Step 3: Converting to C# Program ===" -ForegroundColor Yellow

$outputCsPath = Join-Path $convertedDir "program.cs"
$convertArgs = @("convert", $MppgScriptPath, "--output-file", $outputCsPath)

$convertPsi = New-Object System.Diagnostics.ProcessStartInfo
$convertPsi.FileName = "dotnet"
$convertPsi.Arguments = "$cliDllPath $($convertArgs -join ' ')"
$convertPsi.WorkingDirectory = $convertedDir
$convertPsi.UseShellExecute = $false
$convertPsi.RedirectStandardOutput = $true
$convertPsi.RedirectStandardError = $true
$convertPsi.CreateNoWindow = $true

$convertProc = [System.Diagnostics.Process]::Start($convertPsi)
$convertStdOut = $convertProc.StandardOutput.ReadToEnd()
$convertStdErr = $convertProc.StandardError.ReadToEnd()
$convertProc.WaitForExit()

if ($convertProc.ExitCode -ne 0) {
    Write-Host "Convert stdout: $convertStdOut" -ForegroundColor Red
    Write-Host "Convert stderr: $convertStdErr" -ForegroundColor Red
    throw "Convert failed with exit code $($convertProc.ExitCode)"
}

Write-Host "Converted to: $outputCsPath" -ForegroundColor Green

$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>14</LangVersion>
    <Features>FileBasedProgram</Features>
    <NoWarn>CS9298</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$cliProject" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="$mappingProject" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Unipi.Nancy.Expressions" Version="1.0.1" />
  </ItemGroup>
</Project>
"@

$generatedCsproj = Join-Path $convertedDir "ConvertedProject.csproj"
$csprojContent | Set-Content $generatedCsproj

$convertProjectDir = Join-Path $convertedDir "ConvertedProject"
New-Item -ItemType Directory -Path $convertProjectDir -Force | Out-Null
Move-Item $outputCsPath $convertProjectDir\program.cs -Force
Move-Item $generatedCsproj $convertProjectDir\ConvertedProject.csproj -Force

Write-Host "`n=== Step 4: Building Converted Program ===" -ForegroundColor Yellow

$buildOutput = dotnet build $convertProjectDir\ConvertedProject.csproj -c Release -f net10.0 --nologo -v q -p:NoWarn=CS9298 2>&1
if ($LASTEXITCODE -ne 0) { throw "Converted program build failed: $buildOutput" }
Write-Host "Converted program build complete" -ForegroundColor Green

$convertedDll = Join-Path $convertProjectDir "bin\Release\net10.0\ConvertedProject.dll"

Write-Host "`n=== Step 5: Benchmarking Converted Program ===" -ForegroundColor Yellow

$convResult = Measure-Benchmark -ExePath $convertedDll -WorkingDir $convertProjectDir -Arguments @() -Iterations 3

Write-Host "  Avg Time: $($convResult.AvgTimeMs) ms" -ForegroundColor Cyan
Write-Host "  Avg Memory: $($convResult.AvgMemoryMB) MB" -ForegroundColor Cyan

$convTime = $convResult.AvgTimeMs
$convMem = $convResult.AvgMemoryMB

Write-Host "`n=== Comparison Table ===" -ForegroundColor Green

$table = @"
+---------------------------+------------------+------------------+
| Metric                    | Nancy-Playground | Converted C#     |
+---------------------------+------------------+------------------+
| Runtime (ms)              | $($cliTime.ToString().PadLeft(16)) | $($convTime.ToString().PadLeft(16)) |
| Memory (MB)               | $($cliMem.ToString().PadLeft(16)) | $($convMem.ToString().PadLeft(16)) |
+---------------------------+------------------+------------------+
"@

Write-Host $table

$summaryFile = Join-Path $scriptDir "benchmark-results-$([System.IO.Path]::GetFileNameWithoutExtension($MppgScriptPath))-$($benchmarkDate.ToString("yyyyMMdd-HH-mm-ss")).txt"
$summary = @"
Benchmark Results
=================
Script: $MppgScriptPath
Date: $($benchmarkDate.ToString("yyyy-MM-dd HH:mm:ss"))

+------------------+------------------+
| Nancy-Playground | Converted C#     |
+------------------+------------------+
| $($cliTime.ToString().PadLeft(13)) ms | $($convTime.ToString().PadLeft(13)) ms |
| $($cliMem.ToString().PadLeft(13)) MB | $($convMem.ToString().PadLeft(13)) MB |
+------------------+------------------+
"@
$summary | Set-Content $summaryFile
Write-Host "`nResults saved to: $summaryFile" -ForegroundColor Gray

Remove-Item -Path $workingDir -Recurse -Force

Write-Host "`nBenchmark complete!" -ForegroundColor Green