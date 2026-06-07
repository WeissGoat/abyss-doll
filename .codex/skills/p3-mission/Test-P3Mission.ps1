param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [switch]$Strict
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "scripts\p3_mission.py"

$argsList = @("validate", "--path", $Path)
if ($Strict) {
    $argsList += "--strict"
}

python $pythonScript @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
