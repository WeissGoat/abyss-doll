param(
    [string]$RequestCatalogPath = "",
    [string[]]$VisualID = @(),
    [string]$OutputPath = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$argsList = @((Join-Path $scriptDir "export_art_prompt_authoring_package.py"))
if (-not [string]::IsNullOrWhiteSpace($RequestCatalogPath)) {
    $argsList += @("--request-catalog", $RequestCatalogPath)
}
foreach ($id in $VisualID) {
    $argsList += @("--visual-id", $id)
}
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $argsList += @("--output-path", $OutputPath)
}
if ($DryRun) { $argsList += "--dry-run" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
