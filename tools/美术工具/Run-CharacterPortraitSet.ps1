param(
    [Parameter(Mandatory = $true)][string]$AssetSetID,
    [string]$ManifestPath = "",
    [string]$RequestCatalogPath = "",
    [ValidateSet("auto", "natural_language_v2", "danbooru_tags_v2")][string]$PromptFormat = "auto",
    [string[]]$VisualID = @(),
    [string]$Provider = "",
    [string]$Config = "",
    [int]$Variants = 2,
    [int]$ExecuteLimit = 1,
    [string]$ProductionRunID = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$argsList = @(
    (Join-Path $scriptDir "run_character_portrait_set.py"),
    "--asset-set-id", $AssetSetID,
    "--prompt-format", $PromptFormat,
    "--variants", $Variants,
    "--execute-limit", $ExecuteLimit
)
if ($ManifestPath) { $argsList += @("--manifest-path", $ManifestPath) }
if ($RequestCatalogPath) { $argsList += @("--request-catalog-path", $RequestCatalogPath) }
foreach ($id in $VisualID) { $argsList += @("--visual-id", $id) }
if ($Provider) { $argsList += @("--provider", $Provider) }
if ($Config) { $argsList += @("--config", $Config) }
if ($ProductionRunID) { $argsList += @("--production-run-id", $ProductionRunID) }
if ($DryRun) { $argsList += "--dry-run" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
