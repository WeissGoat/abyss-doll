param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("zero_stand_neutral", "zero_dialogue_neutral", "zero_maintenance_sit")]
    [string]$Asset,

    [Parameter(Mandatory = $true)]
    [string[]]$Reference,

    [int]$Count = 1,
    [string]$OutputDir,
    [string]$ConfigPath = "tools/ai-image-gateway/config.local.yaml"
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "zero_portrait_master_batch.py"
$argsList = @($scriptPath, "--config", $ConfigPath, "--asset", $Asset, "--count", $Count)
foreach ($referencePath in $Reference) {
    $argsList += "--reference"
    $argsList += $referencePath
}
if ($OutputDir) {
    $argsList += "--output-dir"
    $argsList += $OutputDir
}

python @argsList
