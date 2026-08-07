param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Prepare", "Finalize", "Check")]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$AssetSetID,
    [string]$ProductionRunID = "",
    [string]$ReviewPath = "",
    [string]$BaselineMap = "",
    [string]$ManifestPath = "美术文档/_generated/art_manifest.json",
    [string]$IncomingRoot = "UnityClient/Assets/Art/_IncomingAI",
    [string]$EvidenceRoot = "UnityClient/Logs/P3ArtProduction"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "portrait_set_gate.py"
$argsList = @(
    $pythonScript,
    "--phase", $Phase.ToLowerInvariant(),
    "--manifest-path", $ManifestPath,
    "--incoming-root", $IncomingRoot,
    "--evidence-root", $EvidenceRoot,
    "--asset-set-id", $AssetSetID
)

if ($ProductionRunID -ne "") { $argsList += @("--production-run-id", $ProductionRunID) }
if ($ReviewPath -ne "") { $argsList += @("--review-path", $ReviewPath) }
if ($BaselineMap -ne "") { $argsList += @("--baseline-map", $BaselineMap) }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
