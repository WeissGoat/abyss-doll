param(
    [string]$ManifestPath = "",
    [string]$OutputPath = "",
    [string]$ReportPath = "",
    [string[]]$VisualID = @(),
    [string[]]$RefreshVisualIntentID = @(),
    [switch]$RefreshStyleCatalog,
    [switch]$DryRun,
    [switch]$Overwrite
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "compile_art_generation_requests.py"
$argsList = @($pythonScript)
foreach ($pair in @(
    @("--manifest-path", $ManifestPath),
    @("--output-path", $OutputPath),
    @("--report-path", $ReportPath)
)) {
    if (-not [string]::IsNullOrWhiteSpace($pair[1])) {
        $argsList += $pair
    }
}
foreach ($id in $VisualID) {
    $argsList += @("--visual-id", $id)
}
foreach ($id in $RefreshVisualIntentID) {
    $argsList += @("--refresh-visual-intent-id", $id)
}
if ($RefreshStyleCatalog) { $argsList += "--refresh-style-catalog" }
if ($DryRun) { $argsList += "--dry-run" }
if ($Overwrite) { $argsList += "--overwrite" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
