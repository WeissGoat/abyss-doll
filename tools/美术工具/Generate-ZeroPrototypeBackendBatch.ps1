param(
    [string]$ConfigPath = "tools/ai-image-gateway/config.local.yaml",
    [string[]]$Only,
    [int]$Count = 3,
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "zero_prototype_backend_batch.py"
$argsList = @($scriptPath, "--config", $ConfigPath, "--count", $Count)
if ($Only -and $Only.Count -gt 0) {
    $argsList += "--only"
    $argsList += $Only
}
if ($OutputDir) {
    $argsList += "--output-dir"
    $argsList += $OutputDir
}

python @argsList
