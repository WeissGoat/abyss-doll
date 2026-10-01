param(
    [string]$Path = "",

    [switch]$Latest
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "scripts\p3_mission.py"

if (-not $Latest -and -not $Path) {
    $Latest = $true
}

$argsList = @("next")
if ($Latest) {
    $argsList += "--latest"
} else {
    $argsList += @("--path", $Path)
}

python $pythonScript @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
