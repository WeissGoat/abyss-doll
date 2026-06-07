param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$Id,

    [Parameter(Mandatory = $true)]
    [ValidateSet("TODO", "DOING", "REVIEW", "FIX", "DONE", "BLOCKED")]
    [string]$Status,

    [string]$Evidence = "",

    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "scripts\p3_mission.py"

$argsList = @("update", "--path", $Path, "--id", $Id, "--status", $Status)
if ($Evidence) {
    $argsList += @("--evidence", $Evidence)
}
if ($Notes) {
    $argsList += @("--notes", $Notes)
}

python $pythonScript @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
