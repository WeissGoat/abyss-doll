<#
.SYNOPSIS
  Runs the self-check of a Windows build: P3.exe -p3smoke (see P3BuildSmoke.cs).

.DESCRIPTION
  Starts the build in a 1280x720 window, which screenshots the prologue, skips it, plays three dives through the
  player buttons and quits. Screenshots, P3Smoke.txt and Player.log go to UnityClient/Logs/Playtest/<time>_build_smoke.
  Exit code 0 means PASS.

.EXAMPLE
  .\tools\build\Test-WindowsBuild.ps1                       # newest folder under Builds/Windows
  .\tools\build\Test-WindowsBuild.ps1 -BuildDir Builds\Windows\P3_20261004_1228
#>
param(
    [string]$BuildDir,
    [int]$TimeoutSeconds = 720
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if (-not $BuildDir) {
    $BuildDir = (Get-ChildItem (Join-Path $repoRoot "Builds\Windows") -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}

$exe = Join-Path $BuildDir "P3.exe"
if (-not (Test-Path $exe)) {
    throw "No P3.exe in $BuildDir"
}

$outDir = Join-Path $repoRoot ("UnityClient\Logs\Playtest\" + (Get-Date -Format "yyyyMMdd_HHmm") + "_build_smoke")
New-Item -ItemType Directory -Force $outDir | Out-Null
$arguments = @("-p3smoke", "-p3smokeOut", "`"$outDir`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-logFile", "`"$(Join-Path $outDir 'Player.log')`"")

Write-Host "Running $exe -p3smoke ... output: $outDir"
# The -screen-* arguments would otherwise become the window size of the producer's next normal launch.
$screenPrefs = Join-Path $repoRoot "tools\agent\screen_prefs.py"
python $screenPrefs backup
$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
$finished = $process.WaitForExit($TimeoutSeconds * 1000)
if (-not $finished) {
    Stop-Process -Id $process.Id -Force
    $process.WaitForExit()
}
python $screenPrefs restore
if (-not $finished) {
    throw "Timed out after $TimeoutSeconds s. See $outDir"
}

$resultFile = Join-Path $outDir "P3Smoke.txt"
if (-not (Test-Path $resultFile)) {
    throw "No P3Smoke.txt (exit code $($process.ExitCode)). See $(Join-Path $outDir 'Player.log')"
}

Get-Content $resultFile -TotalCount 1
$errors = Select-String -Path (Join-Path $outDir "Player.log") -Pattern "Exception|\[Error\]|^Error" | Select-Object -First 10
if ($errors) {
    Write-Host "Errors in Player.log:"
    $errors | ForEach-Object { Write-Host "  $($_.Line)" }
}

exit $process.ExitCode
