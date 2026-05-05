param(
    [string]$ConfigRoot = "UnityClient/Assets/StreamingAssets/Configs",
    [string]$ManifestPath = "",
    [string]$MarkdownPath = "",
    [switch]$NoSystemAssets
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "update_art_manifest.py"

$argsList = @(
    $pythonScript,
    "--config-root", $ConfigRoot
)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($MarkdownPath -ne "") {
    $argsList += @("--markdown-path", $MarkdownPath)
}

if ($NoSystemAssets) {
    $argsList += "--no-system-assets"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
