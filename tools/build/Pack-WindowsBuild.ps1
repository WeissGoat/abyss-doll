<#
.SYNOPSIS
  Zips a Windows build folder for sharing, leaving out debug symbols and test-run output.

.EXAMPLE
  .\tools\build\Pack-WindowsBuild.ps1                       # newest folder under Builds/Windows
  .\tools\build\Pack-WindowsBuild.ps1 -BuildDir Builds\Windows\P3_20261004_1244
#>
param([string]$BuildDir)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if (-not $BuildDir) {
    $BuildDir = (Get-ChildItem (Join-Path $repoRoot "Builds\Windows") -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}

$BuildDir = (Resolve-Path $BuildDir).Path
# *_DoNotShip holds Burst debug symbols, which Unity says not to distribute; Logs and P3Smoke come from test runs.
$items = Get-ChildItem $BuildDir | Where-Object { $_.Name -notlike "*_DoNotShip" -and $_.Name -ne "Logs" -and $_.Name -ne "P3Smoke" }
$zipPath = "$BuildDir.zip"
Compress-Archive -Path $items.FullName -DestinationPath $zipPath -Force
Write-Host "Zipped: $zipPath ($([math]::Round((Get-Item $zipPath).Length / 1MB, 1)) MB)"
