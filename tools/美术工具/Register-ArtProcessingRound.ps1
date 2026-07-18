param(
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [Parameter(Mandatory=$true)][string]$VisualID,
    [Parameter(Mandatory=$true)][string]$StagingDirectory,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "register_art_processing_round.py"
$argsList = @($pythonScript, "--visual-id", $VisualID, "--staging-directory", $StagingDirectory)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}
if ($IncomingRoot -ne "") {
    $argsList += @("--incoming-root", $IncomingRoot)
}
if ($DryRun) {
    $argsList += "--dry-run"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
