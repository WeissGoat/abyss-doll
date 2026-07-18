param(
    [string]$ManifestPath = "",
    [string]$PromptMarkdownPath = "",
    [string[]]$VisualID = @(),
    [switch]$Overwrite,
    [switch]$RefreshSpec,
    [switch]$RefreshSpecOnly
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_art_prompts.py"

$argsList = @($pythonScript)

if ($ManifestPath -ne "") {
    $argsList += @("--manifest-path", $ManifestPath)
}

if ($PromptMarkdownPath -ne "") {
    $argsList += @("--prompt-markdown-path", $PromptMarkdownPath)
}

foreach ($id in $VisualID) {
    if ($id -ne "") {
        $argsList += @("--visual-id", $id)
    }
}

if ($Overwrite) {
    $argsList += "--overwrite"
}

if ($RefreshSpec) {
    $argsList += "--refresh-spec"
}

if ($RefreshSpecOnly) {
    $argsList += "--refresh-spec-only"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
