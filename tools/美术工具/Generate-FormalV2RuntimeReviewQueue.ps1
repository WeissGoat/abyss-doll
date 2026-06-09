param(
    [string]$SemanticReviewPath = "",
    [string]$HandoffPath = "",
    [string]$RuntimeStatusPath = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_runtime_review_queue.py"

$argsList = @($pythonScript)

if ($SemanticReviewPath -ne "") {
    $argsList += @("--semantic-review-path", $SemanticReviewPath)
}

if ($HandoffPath -ne "") {
    $argsList += @("--handoff-path", $HandoffPath)
}

if ($RuntimeStatusPath -ne "") {
    $argsList += @("--runtime-status-path", $RuntimeStatusPath)
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
