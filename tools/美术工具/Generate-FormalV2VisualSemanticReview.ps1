param(
    [string]$AssetReviewPath = "",
    [string]$OutputDir = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_visual_semantic_review.py"

$argsList = @($pythonScript)

if ($AssetReviewPath -ne "") {
    $argsList += @("--asset-review-path", $AssetReviewPath)
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
