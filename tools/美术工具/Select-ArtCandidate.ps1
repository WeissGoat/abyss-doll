param(
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [Parameter(Mandatory=$true)][string]$VisualID,
    [Parameter(Mandatory=$true)][string]$ReviewPath,
    [switch]$AllowSelectedOverwrite,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "select_art_candidate.py"
$argsList = @($pythonScript, "--visual-id", $VisualID, "--review-path", $ReviewPath)

if ($ManifestPath -ne "") { $argsList += @("--manifest-path", $ManifestPath) }
if ($IncomingRoot -ne "") { $argsList += @("--incoming-root", $IncomingRoot) }
if ($AllowSelectedOverwrite) { $argsList += "--allow-selected-overwrite" }
if ($DryRun) { $argsList += "--dry-run" }

python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
