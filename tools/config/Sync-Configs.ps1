param(
    [string]$SourceRoot = "",
    [string]$TargetRoot = "UnityClient/Assets/StreamingAssets/Configs",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath

if ($SourceRoot -eq "") {
    $sourcePath = Get-ChildItem -LiteralPath $repoRoot -Directory |
        Where-Object {
            $_.Name.EndsWith("(JSON)") -and
            (Test-Path -LiteralPath (Join-Path $_.FullName "Items")) -and
            (Test-Path -LiteralPath (Join-Path $_.FullName "Dungeons"))
        } |
        Select-Object -First 1 -ExpandProperty FullName

    if ($null -eq $sourcePath) {
        throw "Could not locate the config source directory. Pass -SourceRoot explicitly."
    }
} else {
    $sourcePath = (Resolve-Path (Join-Path $repoRoot $SourceRoot)).ProviderPath
}

$targetPath = Join-Path $repoRoot $TargetRoot
$streamingAssetsPath = Join-Path $repoRoot "UnityClient\Assets\StreamingAssets"

if (-not (Test-Path -LiteralPath $streamingAssetsPath)) {
    New-Item -ItemType Directory -Force -Path $streamingAssetsPath | Out-Null
}

if ($Clean -and (Test-Path -LiteralPath $targetPath)) {
    $resolvedTarget = Resolve-Path -LiteralPath $targetPath
    $resolvedStreamingAssets = Resolve-Path -LiteralPath $streamingAssetsPath
    $targetFullName = $resolvedTarget.ProviderPath
    $allowedPrefix = $resolvedStreamingAssets.ProviderPath.TrimEnd("\") + "\"

    if (-not $targetFullName.StartsWith($allowedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean outside UnityClient/Assets/StreamingAssets: $targetFullName"
    }

    Remove-Item -LiteralPath $targetFullName -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
Get-ChildItem -LiteralPath $sourcePath -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Recurse -Force
}

Write-Host "[Sync-Configs] Synced configs from '$SourceRoot' to '$TargetRoot'."
