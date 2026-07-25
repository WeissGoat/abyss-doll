param(
    [Parameter(Mandatory=$true)][string]$PlanPath,
    [string]$ManifestPath = "",
    [string]$RequestCatalogPath = "",
    [string]$IncomingRoot = "",
    [string]$LogRoot = "",
    [string]$Config = "",
    [string]$Provider = "",
    [string[]]$Route = @(),
    [string[]]$VisualID = @(),
    [string[]]$AssetClass = @(),
    [int]$Limit = 0,
    [string]$ProductionRunID = "",
    [switch]$DryRun,
    [switch]$AllowBlocked
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "run_art_production_batch.py"
$argsList = @($pythonScript, "--plan-path", $PlanPath)

foreach ($pair in @(
    @("--manifest-path", $ManifestPath),
    @("--request-catalog", $RequestCatalogPath),
    @("--incoming-root", $IncomingRoot),
    @("--log-root", $LogRoot),
    @("--config", $Config),
    @("--provider", $Provider),
    @("--production-run-id", $ProductionRunID)
)) {
    if (-not [string]::IsNullOrWhiteSpace($pair[1])) {
        $argsList += $pair
    }
}
foreach ($item in $Route) { $argsList += @("--route", $item) }
foreach ($item in $VisualID) { $argsList += @("--visual-id", $item) }
foreach ($item in $AssetClass) { $argsList += @("--asset-class", $item) }
if ($Limit -gt 0) { $argsList += @("--limit", $Limit) }
if ($DryRun) { $argsList += "--dry-run" }
if ($AllowBlocked) { $argsList += "--allow-blocked" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
