param(
    [string]$RegistryPath = "UnityClient/Assets/Resources/VisualAssetRegistry.asset",
    [string]$GapPath,
    [string]$OutputAssetPath,
    [string]$OutputReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($GapPath)) {
    $GapPath = [string]::Concat(
        [char]0x7F8E, [char]0x672F, [char]0x6587, [char]0x6863,
        "/_generated/VisualAssetRegistry",
        [char]0x767B, [char]0x8BB0, [char]0x7F3A, [char]0x53E3,
        [char]0x6E05, [char]0x5355,
        ".json"
    )
}

if ([string]::IsNullOrWhiteSpace($OutputReportPath)) {
    $OutputReportPath = [string]::Concat(
        [char]0x7F8E, [char]0x672F, [char]0x6587, [char]0x6863,
        "/_generated/VisualAssetRegistry",
        [char]0x79BB, [char]0x7EBF, [char]0x5019, [char]0x9009,
        [char]0x62A5, [char]0x544A,
        ".md"
    )
}

if ([string]::IsNullOrWhiteSpace($OutputAssetPath)) {
    $OutputAssetPath = [string]::Concat(
        [char]0x7F8E, [char]0x672F, [char]0x6587, [char]0x6863,
        "/_generated/VisualAssetRegistry.offline_candidate.asset"
    )
}

function Read-Utf8NoBomText([string]$Path) {
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path), [System.Text.Encoding]::UTF8)
}

function Write-Utf8NoBomText([string]$Path, [string]$Text) {
    $fullPath = Join-Path (Get-Location) $Path
    $parent = Split-Path -Parent $fullPath
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($fullPath, $Text, $encoding)
}

function Get-MetaGuid([string]$MetaPath) {
    if (-not (Test-Path -LiteralPath $MetaPath)) {
        throw "Meta file not found: $MetaPath"
    }

    $metaText = Read-Utf8NoBomText $MetaPath
    $match = [regex]::Match($metaText, "(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
    if (-not $match.Success) {
        throw "Unity guid not found in meta file: $MetaPath"
    }

    return $match.Groups[1].Value.ToLowerInvariant()
}

if (-not (Test-Path -LiteralPath $RegistryPath)) {
    throw "Registry asset not found: $RegistryPath"
}
if (-not (Test-Path -LiteralPath $GapPath)) {
    throw "Registry gap checklist not found: $GapPath"
}

$registryText = Read-Utf8NoBomText $RegistryPath
$gap = Get-Content -LiteralPath $GapPath -Encoding UTF8 -Raw | ConvertFrom-Json

$shapeMatch = [regex]::Match(
    $registryText,
    "(?ms)^(?<prefix>.*?^  Entries:\r?\n)(?<entries>.*?)(?<suffix>^  MissingSprite:.*)$"
)
if (-not $shapeMatch.Success) {
    throw "Unsupported VisualAssetRegistry YAML shape. Expected Entries followed by MissingSprite."
}

$entryPattern = "(?ms)^  - VisualID:\s*(?<id>[^\r\n]+)\r?\n    Sprite:\s*\{fileID:\s*21300000,\s*guid:\s*(?<guid>[0-9a-fA-F]{32}),\s*type:\s*3\}\r?\n    Prefab:\s*\{fileID:\s*0\}\r?\n    AudioClip:\s*\{fileID:\s*0\}\r?\n    Material:\s*\{fileID:\s*0\}\r?\n"
$entries = [ordered]@{}
foreach ($match in [regex]::Matches($shapeMatch.Groups["entries"].Value, $entryPattern)) {
    $visualID = $match.Groups["id"].Value.Trim()
    $guid = $match.Groups["guid"].Value.ToLowerInvariant()
    if (-not $entries.Contains($visualID)) {
        $entries[$visualID] = $guid
    }
}

$added = 0
$warnings = New-Object System.Collections.Generic.List[string]
foreach ($item in $gap.Items) {
    if ($item.RequiredAction -ne "register_visual_id") {
        continue
    }

    $visualID = [string]$item.VisualID
    if ([string]::IsNullOrWhiteSpace($visualID)) {
        $warnings.Add("Skipped empty VisualID entry.")
        continue
    }

    if ($entries.Contains($visualID)) {
        $warnings.Add("Already present, skipped: $visualID")
        continue
    }

    $approvedPath = [string]$item.ApprovedPath
    $metaPath = [string]$item.MetaPath
    if (-not (Test-Path -LiteralPath $approvedPath)) {
        throw "Approved PNG missing for ${visualID}: $approvedPath"
    }

    $entries[$visualID] = Get-MetaGuid $metaPath
    $added++
}

$entryLines = New-Object System.Collections.Generic.List[string]
foreach ($visualID in ($entries.Keys | Sort-Object)) {
    $guid = $entries[$visualID]
    $entryLines.Add("  - VisualID: $visualID")
    $entryLines.Add("    Sprite: {fileID: 21300000, guid: $guid, type: 3}")
    $entryLines.Add("    Prefab: {fileID: 0}")
    $entryLines.Add("    AudioClip: {fileID: 0}")
    $entryLines.Add("    Material: {fileID: 0}")
}

$candidateText = $shapeMatch.Groups["prefix"].Value -replace "`r`n", "`n"
$candidateText += (($entryLines -join "`n") + "`n")
$candidateText += ($shapeMatch.Groups["suffix"].Value -replace "`r`n", "`n")
Write-Utf8NoBomText $OutputAssetPath $candidateText

$summary = $gap.Summary
$generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
$reportLines = @(
    "# VisualAssetRegistry Offline Candidate Report",
    "",
    "- GeneratedAt: ``$generatedAt``",
    "- Source registry: ``$RegistryPath``",
    "- Gap checklist: ``$GapPath``",
    "- Candidate asset: ``$OutputAssetPath``",
    "- ProgramIntegrateVisualCount: ``$($summary.ProgramIntegrateVisualCount)``",
    "- MissingRegistryCount input: ``$($summary.MissingRegistryCount)``",
    "- Existing entries parsed: ``$($entries.Count - $added)``",
    "- Entries added to candidate: ``$added``",
    "- Candidate total entries: ``$($entries.Count)``",
    "- Existing live entries changed: ``0``",
    "- Applied to Unity registry: ``false``",
    "",
    "This file is an offline candidate only. It does not replace `UnityClient/Assets/Resources/VisualAssetRegistry.asset`.",
    "Program side should review/diff it or run the Unity menu rebuild in a licensed Editor session.",
    ""
)
if ($warnings.Count -gt 0) {
    $reportLines += "## Warnings"
    $reportLines += ""
    foreach ($warning in $warnings) {
        $reportLines += "- $warning"
    }
    $reportLines += ""
}

Write-Utf8NoBomText $OutputReportPath ($reportLines -join "`n")

Write-Host "[OK] Offline candidate generated."
Write-Host "[OK] Added=$added Total=$($entries.Count)"
Write-Host "[OK] Candidate=$OutputAssetPath"
Write-Host "[OK] Report=$OutputReportPath"
