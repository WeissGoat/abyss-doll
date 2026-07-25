param(
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [Parameter(Mandatory=$true)][string]$VisualID,
    [Parameter(Mandatory=$true)][string]$SourcePath,
    [string]$DestinationName = "r01_001.png",
    [Parameter(Mandatory=$true)][string]$BatchID,
    [string]$SourceReview = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "import_art_candidate.py"
$argsList = @(
    $scriptPath,
    "--visual-id", $VisualID,
    "--source-path", $SourcePath,
    "--destination-name", $DestinationName,
    "--batch-id", $BatchID
)
if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}
if ($IncomingRoot -ne "") {
    $argsList += @("--incoming-root", $IncomingRoot)
}
if ($SourceReview -ne "") {
    $argsList += @("--source-review", $SourceReview)
}
if ($DryRun) {
    $argsList += "--dry-run"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
