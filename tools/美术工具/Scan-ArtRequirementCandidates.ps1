param(
    [string]$ManifestPath = "",
    [string]$SeedPath = "",
    [string]$ApprovedRoot = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [string]$SnapshotDir = "",
    [string]$SnapshotTag = "",
    [string[]]$ScanRoot = @(),
    [int]$MaxTextReviews = 80,
    [int]$MaxSourcesPerItem = 5,
    [switch]$Snapshot
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "scan_art_requirement_candidates.py"

$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($SeedPath -ne "") {
    $argsList += @("--seed-path", $SeedPath)
}

if ($ApprovedRoot -ne "") {
    $argsList += @("--approved-root", $ApprovedRoot)
}

if ($OutputJson -ne "") {
    $argsList += @("--output-json", $OutputJson)
}

if ($OutputMarkdown -ne "") {
    $argsList += @("--output-markdown", $OutputMarkdown)
}

if ($SnapshotDir -ne "") {
    $argsList += @("--snapshot-dir", $SnapshotDir)
}

if ($SnapshotTag -ne "") {
    $argsList += @("--snapshot-tag", $SnapshotTag)
}

foreach ($root in $ScanRoot) {
    if ($root -ne "") {
        $argsList += @("--scan-root", $root)
    }
}

$argsList += @("--max-text-reviews", $MaxTextReviews)
$argsList += @("--max-sources-per-item", $MaxSourcesPerItem)

if ($Snapshot) {
    $argsList += "--snapshot"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
