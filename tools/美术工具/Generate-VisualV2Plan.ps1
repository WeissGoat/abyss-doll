param(
    [string]$QualityBacklogPath = "",
    [string]$ManifestPath = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [string]$BatchID = "",
    [string]$Provider = "novelai",
    [int]$Variants = 4,
    [double]$DelaySeconds = 1.0,
    [string]$LastProbeNote = "",
    [string]$LastProbeBatchID = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_visual_v2_plan.py"

$argsList = @($pythonScript)

if ($QualityBacklogPath -ne "") {
    $argsList += @("--quality-backlog-path", $QualityBacklogPath)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($OutputJson -ne "") {
    $argsList += @("--output-json", $OutputJson)
}

if ($OutputMarkdown -ne "") {
    $argsList += @("--output-markdown", $OutputMarkdown)
}

if ($SnapshotDir -ne "") {
    $argsList += @("--snapshot-dir", $SnapshotDir)
}

if ($SnapshotTag -ne "") {
    $argsList += @("--snapshot-tag", $SnapshotTag)
}

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

if ($Provider -ne "") {
    $argsList += @("--provider", $Provider)
}

if ($Variants -gt 0) {
    $argsList += @("--variants", $Variants)
}

$argsList += @("--delay-seconds", $DelaySeconds)

if ($LastProbeNote -ne "") {
    $argsList += @("--last-probe-note", $LastProbeNote)
}

if ($LastProbeBatchID -ne "") {
    $argsList += @("--last-probe-batch-id", $LastProbeBatchID)
}

if ($Snapshot) {
    $argsList += "--snapshot"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
