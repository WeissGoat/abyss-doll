param(
    [string]$RequestCatalogPath = "",
    [string]$ManifestPath = "",
    [Parameter(Mandatory = $true)][string]$RevisionPath,
    [switch]$NoActivate,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$argsList = @(
    (Join-Path $scriptDir "publish_art_prompt_revision.py"),
    "--revision-path", $RevisionPath
)
if (-not [string]::IsNullOrWhiteSpace($RequestCatalogPath)) {
    $argsList += @("--request-catalog", $RequestCatalogPath)
}
if (-not [string]::IsNullOrWhiteSpace($ManifestPath)) {
    $argsList += @("--manifest-path", $ManifestPath)
}
if ($NoActivate) { $argsList += "--no-activate" }
if ($DryRun) { $argsList += "--dry-run" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
