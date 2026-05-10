param(
    [string]$ManifestPath = "",
    [string]$InRoot = "",
    [string]$Status = "",
    [string[]]$Domain = @(),
    [string[]]$VisualID = @(),
    [string[]]$Priority = @(),
    [string]$BatchID = "",
    [int]$Limit = 0,
    [switch]$AllowProcessedFallback,
    [switch]$DryRun,
    [switch]$Overwrite
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "sync_approved_art.py"

$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($InRoot -ne "") {
    $argsList += @("--in-root", $InRoot)
}

if ($Status -ne "") {
    $argsList += @("--status", $Status)
}

foreach ($item in $Domain) {
    $argsList += @("--domain", $item)
}

foreach ($item in $VisualID) {
    $argsList += @("--visual-id", $item)
}

foreach ($item in $Priority) {
    $argsList += @("--priority", $item)
}

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

if ($Limit -gt 0) {
    $argsList += @("--limit", $Limit)
}

if ($AllowProcessedFallback) {
    $argsList += "--allow-processed-fallback"
}

if ($DryRun) {
    $argsList += "--dry-run"
}

if ($Overwrite) {
    $argsList += "--overwrite"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
