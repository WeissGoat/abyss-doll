param(
    [string]$HandoffPath = "",
    [string]$ManifestPath = "",
    [string]$OutputDir = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_asset_review.py"

$argsList = @($pythonScript)

if ($HandoffPath -ne "") {
    $argsList += @("--handoff-path", $HandoffPath)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($OutputDir -ne "") {
    $argsList += @("--output-dir", $OutputDir)
}

if ($SnapshotDir -ne "") {
    $argsList += @("--snapshot-dir", $SnapshotDir)
}

if ($SnapshotTag -ne "") {
    $argsList += @("--snapshot-tag", $SnapshotTag)
}

if ($Snapshot) {
    $argsList += "--snapshot"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
