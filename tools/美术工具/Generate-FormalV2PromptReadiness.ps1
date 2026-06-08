param(
    [string]$ManifestPath = "",
    [string]$HandoffPath = "",
    [string]$OutputDir = "",
    [string]$SnapshotTag = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_prompt_readiness.py"

$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($HandoffPath -ne "") {
    $argsList += @("--handoff-path", $HandoffPath)
}

if ($OutputDir -ne "") {
    $argsList += @("--output-dir", $OutputDir)
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
