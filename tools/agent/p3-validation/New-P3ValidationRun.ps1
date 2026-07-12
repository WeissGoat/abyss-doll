[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProfileId,
    [string]$RunId = "",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath,
    [switch]$PassThru
)
$ErrorActionPreference = "Stop"
if ($RunId -eq "") { $RunId = "{0}_{1}" -f (Get-Date -Format "yyyyMMdd_HHmmss"), $ProfileId }
if ($RunId -notmatch '^[A-Za-z0-9_-]+$') { throw "invalid RunId: $RunId" }
$profiles = (Get-Content (Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json") -Raw -Encoding UTF8 | ConvertFrom-Json).profiles
$profile = $profiles | Where-Object id -eq $ProfileId | Select-Object -First 1
if (-not $profile) { throw "unknown ProfileId: $ProfileId" }
$root = Join-Path $RepoRoot "UnityClient/Logs/P3Validation/runs/$RunId"
if (Test-Path (Join-Path $root "validation-summary.json")) { throw "RunId already completed: $RunId" }
if (Test-Path (Join-Path $root "request.json")) { throw "RunId already exists: $RunId" }
foreach ($child in @("steps", "unity", "screenshots", "source_reports")) { New-Item -ItemType Directory -Force -Path (Join-Path $root $child) | Out-Null }
$request = [ordered]@{ schema_version="p3-validation/request@1"; run_id=$RunId; profile_id=$ProfileId; profile_version=$profile.version; created_at=(Get-Date).ToUniversalTime().ToString("o"); evidence_root=$root }
$requestPath = Join-Path $root "request.json"
$tempPath = "$requestPath.tmp"
$request | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $tempPath -Encoding UTF8
Move-Item -LiteralPath $tempPath -Destination $requestPath
$result = [pscustomobject]@{ RunId=$RunId; ProfileId=$ProfileId; EvidenceRoot=$root; RequestPath=$requestPath }
if ($PassThru) { $result } else { $result | ConvertTo-Json -Depth 4 }
