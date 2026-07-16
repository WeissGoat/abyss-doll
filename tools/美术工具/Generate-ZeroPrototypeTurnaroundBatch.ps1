param(
    [string]$ConfigPath = "tools/ai-image-gateway/config.local.yaml",
    [string[]]$Only,
    [int]$Count = 3,
    [string]$OutputDir,
    [switch]$RefreshOnly
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "zero_prototype_turnaround_batch.py"
$argsList = @($scriptPath, "--config", $ConfigPath, "--count", $Count)
if ($Only -and $Only.Count -gt 0) {
    $argsList += "--only"
    $argsList += $Only
}
if ($OutputDir) {
    $argsList += "--output-dir"
    $argsList += $OutputDir
}
if ($RefreshOnly) {
    $argsList += "--refresh-only"
}

python @argsList
