param(
    [string]$BatchPlanPath = "",
    [string]$ManifestPath = "",
    [string]$VisualID = "",
    [string]$BatchID = "local_v0_missing_assets",
    [switch]$DryRun,
    [switch]$Overwrite
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_local_v0_art.py"

$argsList = @($pythonScript)

if ($BatchPlanPath -ne "") {
    $argsList += @("--batch-plan-path", $BatchPlanPath)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($VisualID -ne "") {
    $argsList += @("--visual-id", $VisualID)
}

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

if ($DryRun) {
    $argsList += "--dry-run"
}

if ($Overwrite) {
    $argsList += "--overwrite"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
