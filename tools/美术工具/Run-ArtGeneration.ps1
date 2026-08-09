param(
    [string]$Config = "",
    [string]$ManifestPath = "",
    [string]$OutRoot = "",
    [string]$RequestCatalog = "",
    [string]$RequestID = "",
    [string]$PromptRevisionID = "",
    [ValidateSet("auto", "natural_language_v2", "danbooru_tags_v2")]
    [string]$PromptFormat = "auto",
    [string]$Provider = "",
    [string]$Status = "",
    [string[]]$Domain = @(),
    [string[]]$VisualID = @(),
    [string[]]$ConfigID = @(),
    [string[]]$Priority = @(),
    [int]$Limit = 0,
    [int]$Variants = 4,
    [int]$Seed = -1,
    [int]$Concurrency = 1,
    [double]$DelaySeconds = 1.0,
    [string]$BatchID = "",
    [string[]]$Extra = @(),
    [string[]]$ReferenceImage = @(),
    [string[]]$ReferenceImageSHA256 = @(),
    [string[]]$ReferenceImageRole = @(),
    [switch]$DryRun,
    [switch]$Overwrite,
    [switch]$PreserveStatus,
    [switch]$SkipIntegrationCandidates
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "run_art_generation.py"

$argsList = @($pythonScript)

if ($Config -ne "") {
    $argsList += @("--config", $Config)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($OutRoot -ne "") {
    $argsList += @("--out-root", $OutRoot)
}

if ($RequestCatalog -ne "") {
    $argsList += @("--request-catalog", $RequestCatalog)
}

if ($RequestID -ne "") {
    $argsList += @("--request-id", $RequestID)
}

if ($PromptRevisionID -ne "") {
    $argsList += @("--prompt-revision-id", $PromptRevisionID)
}

$argsList += @("--prompt-format", $PromptFormat)

if ($Provider -ne "") {
    $argsList += @("--provider", $Provider)
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

foreach ($item in $ConfigID) {
    $argsList += @("--config-id", $item)
}

foreach ($item in $Priority) {
    $argsList += @("--priority", $item)
}

if ($Limit -gt 0) {
    $argsList += @("--limit", $Limit)
}

if ($Variants -gt 0) {
    $argsList += @("--variants", $Variants)
}

if ($Seed -ge 0) {
    $argsList += @("--seed", $Seed)
}

if ($Concurrency -gt 0) {
    $argsList += @("--concurrency", $Concurrency)
}

$argsList += @("--delay-seconds", $DelaySeconds)

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

foreach ($item in $Extra) {
    $argsList += @("--extra", $item)
}

foreach ($item in $ReferenceImage) {
    $argsList += @("--reference-image", $item)
}

foreach ($item in $ReferenceImageSHA256) {
    $argsList += @("--reference-image-sha256", $item)
}

foreach ($item in $ReferenceImageRole) {
    $argsList += @("--reference-image-role", $item)
}

if ($DryRun) {
    $argsList += "--dry-run"
}

if ($Overwrite) {
    $argsList += "--overwrite"
}

if ($PreserveStatus) {
    $argsList += "--preserve-status"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not $DryRun -and -not $SkipIntegrationCandidates) {
    $candidateScript = Join-Path $scriptDir "Generate-ArtIntegrationCandidates.ps1"
    $snapshotTag = "generation"
    if ($BatchID -ne "") {
        $snapshotTag = "generation_$BatchID"
    }
    & $candidateScript -Snapshot -SnapshotTag $snapshotTag
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
