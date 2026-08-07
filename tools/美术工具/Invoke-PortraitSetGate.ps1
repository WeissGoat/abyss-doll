param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Prepare", "Finalize", "Check")]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$AssetSetID,
    [string]$ProductionRunID = "",
    [string]$ReviewPath = "",
    [string]$BaselineMap = "",
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "UnityClient/Assets/Art/_IncomingAI",
    [string]$EvidenceRoot = "UnityClient/Logs/P3ArtProduction"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
$pythonScript = Join-Path $scriptDir "portrait_set_gate.py"
$effectiveManifestPath = $ManifestPath
if ($effectiveManifestPath -eq "") {
    $candidates = @(Get-ChildItem -LiteralPath $projectRoot -Recurse -File -Filter "art_manifest.json" |
        Where-Object { $_.Directory.Name -eq "_generated" })
    if ($candidates.Count -ne 1) {
        throw "Expected exactly one generated art_manifest.json under the project root; pass -ManifestPath explicitly."
    }
    $effectiveManifestPath = $candidates[0].FullName
}
$argsList = @(
    $pythonScript,
    "--phase", $Phase.ToLowerInvariant(),
    "--manifest-path", $effectiveManifestPath,
    "--incoming-root", $IncomingRoot,
    "--evidence-root", $EvidenceRoot,
    "--asset-set-id", $AssetSetID
)

if ($ProductionRunID -ne "") { $argsList += @("--production-run-id", $ProductionRunID) }
if ($ReviewPath -ne "") { $argsList += @("--review-path", $ReviewPath) }
if ($BaselineMap -ne "") { $argsList += @("--baseline-map", $BaselineMap) }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
