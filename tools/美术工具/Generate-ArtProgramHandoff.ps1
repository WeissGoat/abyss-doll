param(
    [string]$IntegrationCandidatesPath = "",
    [string]$AcceptanceQueuePath = "",
    [string]$ManifestPath = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_art_program_handoff.py"

$argsList = @($pythonScript)

if ($IntegrationCandidatesPath -ne "") {
    $argsList += @("--integration-candidates-path", $IntegrationCandidatesPath)
}

if ($AcceptanceQueuePath -ne "") {
    $argsList += @("--acceptance-queue-path", $AcceptanceQueuePath)
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

if ($Snapshot) {
    $argsList += "--snapshot"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
