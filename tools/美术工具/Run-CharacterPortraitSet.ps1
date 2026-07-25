param(
    [Parameter(Mandatory = $true)][string]$AssetSetID,
    [string]$ManifestPath = "美术文档/_generated/art_manifest.json",
    [string]$RequestCatalogPath = "美术文档/_generated/art_generation_requests.json",
    [ValidateSet("auto", "natural_language_v1", "danbooru_tags_v1")][string]$PromptFormat = "auto",
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
    "--manifest-path", $ManifestPath,
    "--request-catalog-path", $RequestCatalogPath,
    "--prompt-format", $PromptFormat,
    "--variants", $Variants,
    "--execute-limit", $ExecuteLimit
)
foreach ($id in $VisualID) { $argsList += @("--visual-id", $id) }
if ($Provider) { $argsList += @("--provider", $Provider) }
if ($Config) { $argsList += @("--config", $Config) }
if ($ProductionRunID) { $argsList += @("--production-run-id", $ProductionRunID) }
if ($DryRun) { $argsList += "--dry-run" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
