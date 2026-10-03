param(
    [switch]$Strict,
    [switch]$Brief
)

$ErrorActionPreference = "Stop"

# agent_health_check.py prints UTF-8; decode it as UTF-8 instead of the console's GBK code page.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "agent_health_check.py"

$argsList = @()
if ($Strict) {
    $argsList += "--strict"
}
if ($Brief) {
    $argsList += "--brief"
}

python $pythonScript @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
