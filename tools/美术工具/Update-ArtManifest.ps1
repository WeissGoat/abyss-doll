param(
    [string]$ConfigRoot = "UnityClient/Assets/StreamingAssets/Configs",
    [string]$ManifestPath = "",
    [string]$MarkdownPath = "",
    [string]$PresetPath = "",
    [switch]$NoSystemAssets,
    [switch]$DryRun
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

if ($PresetPath -ne "") {
    $argsList += @("--preset-path", $PresetPath)
}

if ($NoSystemAssets) {
    $argsList += "--no-system-assets"
}

if ($DryRun) {
    $argsList += "--dry-run"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
