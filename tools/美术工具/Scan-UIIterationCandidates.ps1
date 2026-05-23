param(
    [string]$ScreensPath = "",
    [string]$ManifestPath = "",
    [string]$AcceptanceRoot = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "scan_ui_iteration_candidates.py"

$argsList = @($pythonScript)

if ($ScreensPath -ne "") {
    $argsList += @("--screens-path", $ScreensPath)
}

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($AcceptanceRoot -ne "") {
    $argsList += @("--acceptance-root", $AcceptanceRoot)
}

if ($OutputJson -ne "") {
    $argsList += @("--output-json", $OutputJson)
}

if ($OutputMarkdown -ne "") {
    $argsList += @("--output-markdown", $OutputMarkdown)
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
