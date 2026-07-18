param(
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [switch]$Execute
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "migrate_art_processed_rounds.py"
$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}
if ($IncomingRoot -ne "") {
    $argsList += @("--incoming-root", $IncomingRoot)
}
if ($Execute) {
    $argsList += "--execute"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
