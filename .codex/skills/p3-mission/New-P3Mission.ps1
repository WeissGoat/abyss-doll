param(
    [Parameter(Mandatory = $true)]
    [string]$Goal,

    [Parameter(Mandatory = $true)]
    [string]$SourceSpec,

    [string]$Title = "",

    [string]$Role = "global",

    [string]$Output = "",

    [switch]$Force
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "scripts\p3_mission.py"

$argsList = @("new", "--goal", $Goal, "--source-spec", $SourceSpec, "--role", $Role)
if ($Title) {
    $argsList += @("--title", $Title)
}
if ($Output) {
    $argsList += @("--output", $Output)
}
if ($Force) {
    $argsList += "--force"
}

python $pythonScript @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
