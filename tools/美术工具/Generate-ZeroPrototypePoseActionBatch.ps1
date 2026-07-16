param(
    [string]$ConfigPath = "tools/ai-image-gateway/config.local.yaml",
    [string[]]$Backend = @("gemini_nanobanana"),
    [string[]]$Pose,
    [int]$Count = 1,
    [string]$OutputDir,
    [switch]$RefreshOnly
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "zero_prototype_pose_action_batch.py"
$argsList = @($scriptPath, "--config", $ConfigPath, "--count", $Count)
if ($Backend -and $Backend.Count -gt 0) {
    $argsList += "--backend"
    $argsList += $Backend
}
if ($Pose -and $Pose.Count -gt 0) {
    $argsList += "--pose"
    $argsList += $Pose
}
if ($OutputDir) {
    $argsList += "--output-dir"
    $argsList += $OutputDir
}
if ($RefreshOnly) {
    $argsList += "--refresh-only"
}

python @argsList
