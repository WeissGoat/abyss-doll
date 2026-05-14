param(
    [string]$TokensPath = "",
    [string]$ComponentsPath = "",
    [string]$ScreensPath = "",
    [string]$SeedPath = "",
    [string]$ManifestPath = "",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "validate_ui_design.py"

$argsList = @($pythonScript)

if ($TokensPath -ne "") {
    $argsList += @("--tokens-path", $TokensPath)
}

if ($ComponentsPath -ne "") {
    $argsList += @("--components-path", $ComponentsPath)
}

if ($ScreensPath -ne "") {
    $argsList += @("--screens-path", $ScreensPath)
}

if ($SeedPath -ne "") {
    $argsList += @("--seed-path", $SeedPath)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($OutputPath -ne "") {
    $argsList += @("--output-path", $OutputPath)
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
