param(
    [string]$ReportPath = "",
    [string]$RegistryGapPath = "",
    [string]$HandoffPath = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [string]$SnapshotTag = "",
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_runtime_acceptance_status.py"

$argsList = @($pythonScript)

if ($ReportPath -ne "") {
    $argsList += @("--report-path", $ReportPath)
}

if ($RegistryGapPath -ne "") {
    $argsList += @("--registry-gap-path", $RegistryGapPath)
}

if ($HandoffPath -ne "") {
    $argsList += @("--handoff-path", $HandoffPath)
}

if ($OutputJson -ne "") {
    $argsList += @("--output-json", $OutputJson)
}

if ($OutputMarkdown -ne "") {
    $argsList += @("--output-markdown", $OutputMarkdown)
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
