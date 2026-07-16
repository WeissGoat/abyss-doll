param(
    [string]$ConfigPath = "tools/ai-image-gateway/config.local.yaml",
    [ValidateSet("chatgpt", "gemini", "novelai")]
    [string[]]$Backend,
    [int]$Attempts = 2,
    [double]$RetryDelaySeconds = 5,
    [int]$Width = 512,
    [int]$Height = 512,
    [int]$Seed = 12345,
    [string]$OutputDir,
    [switch]$CheckConfigOnly
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "test_ai_image_backends.py"
$argsList = @(
    $scriptPath,
    "--config", $ConfigPath,
    "--attempts", $Attempts,
    "--retry-delay-seconds", $RetryDelaySeconds,
    "--width", $Width,
    "--height", $Height,
    "--seed", $Seed
)

if ($Backend -and $Backend.Count -gt 0) {
    $argsList += "--backend"
    $argsList += $Backend
}

if ($OutputDir) {
    $argsList += "--output-dir"
    $argsList += $OutputDir
}

if ($CheckConfigOnly) {
    $argsList += "--check-config-only"
}

python @argsList
exit $LASTEXITCODE
