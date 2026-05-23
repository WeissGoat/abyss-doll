param(
    [string]$ManifestPath = "",
    [string]$ScreensPath = "",
    [string]$RegistryPath = "",
    [string]$IncomingRoot = "",
    [string]$OutputJson = "",
    [string]$OutputMarkdown = "",
    [switch]$IncludeDone
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_art_integration_candidates.py"

$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($ScreensPath -ne "") {
    $argsList += @("--screens-path", $ScreensPath)
}

if ($RegistryPath -ne "") {
    $argsList += @("--registry-path", $RegistryPath)
}

if ($IncomingRoot -ne "") {
    $argsList += @("--incoming-root", $IncomingRoot)
}

if ($OutputJson -ne "") {
    $argsList += @("--output-json", $OutputJson)
}

if ($OutputMarkdown -ne "") {
    $argsList += @("--output-markdown", $OutputMarkdown)
}

if ($IncludeDone) {
    $argsList += "--include-done"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
