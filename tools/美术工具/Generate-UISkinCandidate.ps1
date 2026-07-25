param(
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [string[]]$VisualID = @(),
    [Parameter(Mandatory=$true)][string]$BatchID,
    [int]$Variants = 2,
    [string]$Capability = "deterministic_template",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_ui_skin_candidate.py"
$argsList = @($pythonScript, "--batch-id", $BatchID, "--variants", $Variants, "--capability", $Capability)
if ($ManifestPath -ne "") { $argsList += @("--manifest-path", $ManifestPath) }
if ($IncomingRoot -ne "") { $argsList += @("--incoming-root", $IncomingRoot) }
foreach ($item in $VisualID) { $argsList += @("--visual-id", $item) }
if ($DryRun) { $argsList += "--dry-run" }
python @argsList
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
