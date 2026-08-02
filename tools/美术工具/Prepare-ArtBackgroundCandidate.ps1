param(
    [Parameter(Mandatory=$true)][string]$InputPath,
    [Parameter(Mandatory=$true)][string]$StagingDirectory,
    [ValidateSet("alpha_passthrough", "connected_border", "explicit_mask", "segmentation")]
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$ExpectedInputSHA256,
    [string]$MaskPath = "",
    [int]$Threshold = 34,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "prepare_art_background_candidate.py"
$argsList = @(
    $pythonScript,
    "--input", $InputPath,
    "--staging-dir", $StagingDirectory,
    "--method", $Method,
    "--expected-input-sha256", $ExpectedInputSHA256,
    "--threshold", $Threshold
)
if ($MaskPath -ne "") { $argsList += @("--mask", $MaskPath) }
if ($DryRun) { $argsList += "--dry-run" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
