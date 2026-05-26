param(
    [string]$ManifestPath = "",
    [string]$InRoot = "",
    [string]$Status = "",
    [string[]]$Domain = @(),
    [string[]]$VisualID = @(),
    [string[]]$Priority = @(),
    [string]$BatchID = "",
    [string]$CandidateBatchID = "",
    [string]$QualityTier = "",
    [int]$Limit = 0,
    [switch]$AllowProcessedFallback,
    [switch]$AllowNewTargetWithCandidate,
    [switch]$ClearCandidate,
    [switch]$DryRun,
    [switch]$Overwrite,
    [switch]$SkipIntegrationCandidates
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "sync_approved_art.py"

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

if ($QualityTier -ne "") {
    $argsList += @("--quality-tier", $QualityTier)
}

if ($Limit -gt 0) {
    $argsList += @("--limit", $Limit)
}

if ($AllowProcessedFallback) {
    $argsList += "--allow-processed-fallback"
}

if ($AllowNewTargetWithCandidate) {
    $argsList += "--allow-new-target-with-candidate"
}

if ($ClearCandidate) {
    $argsList += "--clear-candidate"
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
    $snapshotTag = "approved_sync"
    if ($BatchID -ne "") {
        $snapshotTag = "approved_sync_$BatchID"
    }
    if ($CandidateBatchID -ne "") {
        $snapshotTag = "approved_sync_candidate_$CandidateBatchID"
    }
    & $candidateScript -Snapshot -SnapshotTag $snapshotTag
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
