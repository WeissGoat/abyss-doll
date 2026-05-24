param(
    [string]$ManifestPath = "",
    [string]$InRoot = "",
    [string]$Status = "generated",
    [string[]]$Domain = @(),
    [string[]]$VisualID = @(),
    [string[]]$Priority = @(),
    [string]$BatchID = "",
    [string]$CandidateBatchID = "",
    [int]$Limit = 0,
    [int]$ContactSize = 160,
    [int]$BackgroundThreshold = 34,
    [switch]$DryRun,
    [switch]$Overwrite,
    [switch]$SkipIntegrationCandidates
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "optimize_art_assets.py"

$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($InRoot -ne "") {
    $argsList += @("--in-root", $InRoot)
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

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

if ($CandidateBatchID -ne "") {
    $argsList += @("--candidate-batch-id", $CandidateBatchID)
}

if ($Limit -gt 0) {
    $argsList += @("--limit", $Limit)
}

if ($ContactSize -gt 0) {
    $argsList += @("--contact-size", $ContactSize)
}

if ($BackgroundThreshold -gt 0) {
    $argsList += @("--background-threshold", $BackgroundThreshold)
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

if (-not $DryRun -and -not $SkipIntegrationCandidates) {
    $candidateScript = Join-Path $scriptDir "Generate-ArtIntegrationCandidates.ps1"
    $snapshotTag = "processed"
    if ($BatchID -ne "") {
        $snapshotTag = "processed_$BatchID"
    }
    if ($CandidateBatchID -ne "") {
        $snapshotTag = "processed_candidate_$CandidateBatchID"
    }
    & $candidateScript -Snapshot -SnapshotTag $snapshotTag
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
