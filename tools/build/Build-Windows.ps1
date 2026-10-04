<#
.SYNOPSIS
  Builds the P3 Windows player in Unity batch mode and zips it for sharing.

.DESCRIPTION
  Needs the Unity Editor closed: batch mode cannot open a project another Editor has open. With the Editor open,
  use the menu Tools/P3/打包 Windows 试玩版, or Unity MCP: return P3BuildTools.BuildWindows(false);

  Output: Builds/Windows/P3_<yyyyMMdd_HHmm>[_dev]/P3.exe plus a .zip next to the folder.

.EXAMPLE
  .\tools\build\Build-Windows.ps1
  .\tools\build\Build-Windows.ps1 -Development
#>
param(
    [switch]$Development,
    [switch]$NoZip,
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.60f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$projectPath = Join-Path $repoRoot "UnityClient"
$logPath = Join-Path $projectPath "Logs\Build-Windows.log"

if (-not (Test-Path $UnityPath)) {
    throw "Unity not found at $UnityPath. Pass -UnityPath."
}

$lockFile = Join-Path $projectPath "Temp\UnityLockfile"
if (Test-Path $lockFile) {
    try {
        $stream = [System.IO.File]::Open($lockFile, 'Open', 'ReadWrite', 'None')
        $stream.Close()
    } catch {
        throw "The Unity Editor has this project open. Close it, or build from the Editor menu Tools/P3/打包 Windows 试玩版."
    }
}

$arguments = @("-batchmode", "-quit", "-projectPath", "`"$projectPath`"", "-executeMethod", "P3BuildTools.BuildWindowsFromCommandLine", "-logFile", "`"$logPath`"")
if ($Development) {
    $arguments += "-p3Development"
}

Write-Host "Building ($(if ($Development) { 'development' } else { 'playtest' }))... log: $logPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
$resultLine = Select-String -Path $logPath -Pattern "Build (succeeded|failed)" | Select-Object -Last 1
if ($process.ExitCode -ne 0 -or $null -eq $resultLine -or $resultLine.Line -notmatch "Build succeeded") {
    Get-Content $logPath -Tail 40
    throw "Build failed (exit code $($process.ExitCode)). Full log: $logPath"
}

Write-Host $resultLine.Line
$buildDir = Get-ChildItem (Join-Path $repoRoot "Builds\Windows") -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $NoZip) {
    $zipPath = "$($buildDir.FullName).zip"
    # *_DoNotShip holds Burst debug symbols, which Unity says not to distribute.
    $items = Get-ChildItem $buildDir.FullName | Where-Object { $_.Name -notlike "*_DoNotShip" }
    Compress-Archive -Path $items.FullName -DestinationPath $zipPath -Force
    Write-Host "Zipped: $zipPath"
}
