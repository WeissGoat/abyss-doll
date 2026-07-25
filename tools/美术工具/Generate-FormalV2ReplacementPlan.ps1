param(
    [string]$OverviewPath = "",
    [string]$ManifestPath = "",
    [string]$RequestCatalogPath = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [string]$BatchID = "",
    [string[]]$VisualID = @(),
    [int]$Variants = 4,
    [double]$DelaySeconds = 1.0,
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_replacement_plan.py"
$argsList = @($pythonScript)

if ($OverviewPath -ne "") { $argsList += @("--overview-path", $OverviewPath) }
if ($ManifestPath -ne "") { $argsList += @("--manifest-path", $ManifestPath) }
if ($RequestCatalogPath -ne "") { $argsList += @("--request-catalog-path", $RequestCatalogPath) }
if ($OutputJson -ne "") { $argsList += @("--output-json", $OutputJson) }
if ($OutputMarkdown -ne "") { $argsList += @("--output-markdown", $OutputMarkdown) }
if ($SnapshotDir -ne "") { $argsList += @("--snapshot-dir", $SnapshotDir) }
if ($SnapshotTag -ne "") { $argsList += @("--snapshot-tag", $SnapshotTag) }
if ($BatchID -ne "") { $argsList += @("--batch-id", $BatchID) }
foreach ($item in $VisualID) { $argsList += @("--visual-id", $item) }
if ($Variants -gt 0) { $argsList += @("--variants", $Variants) }
$argsList += @("--delay-seconds", $DelaySeconds)
if ($Snapshot) { $argsList += "--snapshot" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
