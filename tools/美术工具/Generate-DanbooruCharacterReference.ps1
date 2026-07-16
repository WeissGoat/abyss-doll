param(
    [string]$OutputDir = "",
    [string]$ReportName = "zero_doll_reference_report.md",
    [string]$RawName = "zero_doll_reference_raw.json",
    [int]$SamplePages = 1,
    [int]$SampleLimit = 200,
    [switch]$Fast,
    [switch]$SkipTopCharacters,
    [string[]]$Keyword = @(),
    [string[]]$Search = @()
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "danbooru_character_reference.py"

$argsList = @(
    $pythonScript,
    "--report-name", $ReportName,
    "--raw-name", $RawName,
    "--sample-pages", $SamplePages,
    "--sample-limit", $SampleLimit
)

if ($OutputDir -ne "") {
    $argsList += @("--output-dir", $OutputDir)
}

if ($Fast) {
    $argsList += "--fast"
}

if ($SkipTopCharacters) {
    $argsList += "--skip-top-characters"
}

foreach ($item in $Keyword) {
    $argsList += @("--keyword", $item)
}

foreach ($item in $Search) {
    $argsList += @("--search", $item)
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
