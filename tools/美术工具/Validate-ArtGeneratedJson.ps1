param(
    [switch]$Strict
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "validate_art_generated_json.py"

$argsList = @($pythonScript)
if ($Strict) {
    $argsList += "--strict"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
