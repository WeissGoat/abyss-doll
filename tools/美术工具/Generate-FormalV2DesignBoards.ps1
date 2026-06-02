param(
    [string]$OutDir = "",
    [switch]$Overwrite
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_formal_v2_design_boards.py"

$argsList = @($pythonScript)
if ($OutDir -ne "") {
    $argsList += @("--out-dir", $OutDir)
}
if ($Overwrite) {
    $argsList += "--overwrite"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
