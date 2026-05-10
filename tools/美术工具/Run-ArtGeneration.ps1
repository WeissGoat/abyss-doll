param(
    [string]$Config = "",
    [string]$ManifestPath = "",
    [string]$OutRoot = "",
    [string]$Provider = "",
    [string]$Status = "",
    [string[]]$Domain = @(),
    [string[]]$VisualID = @(),
    [string[]]$Priority = @(),
    [int]$Limit = 0,
    [int]$Variants = 4,
    [int]$Seed = -1,
    [int]$Concurrency = 1,
    [double]$DelaySeconds = 1.0,
    [string]$BatchID = "",
    [string[]]$Extra = @(),
    [switch]$DryRun,
    [switch]$Overwrite
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "run_art_generation.py"

$argsList = @($pythonScript)

if ($Config -ne "") {
    $argsList += @("--config", $Config)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($OutRoot -ne "") {
    $argsList += @("--out-root", $OutRoot)
}

if ($Provider -ne "") {
    $argsList += @("--provider", $Provider)
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

if ($Limit -gt 0) {
    $argsList += @("--limit", $Limit)
}

if ($Variants -gt 0) {
    $argsList += @("--variants", $Variants)
}

if ($Seed -ge 0) {
    $argsList += @("--seed", $Seed)
}

if ($Concurrency -gt 0) {
    $argsList += @("--concurrency", $Concurrency)
}

$argsList += @("--delay-seconds", $DelaySeconds)

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

foreach ($item in $Extra) {
    $argsList += @("--extra", $item)
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
