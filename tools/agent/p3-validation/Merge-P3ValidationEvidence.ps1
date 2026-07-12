[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RunId,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath,
    [ValidateSet("NotStarted","Required","Passed","Failed")][string]$OwnerValidation = "NotStarted",
    [ValidateSet("NotRequired","Required","Passed","Failed")][string]$ExternalReview = "NotRequired",
    [switch]$PassThru
)
$ErrorActionPreference = "Stop"
if ($RunId -notmatch '^[A-Za-z0-9_-]+$') { throw "invalid RunId: $RunId" }
$root = Join-Path $RepoRoot "UnityClient/Logs/P3Validation/runs/$RunId"
$requestPath = Join-Path $root "request.json"
if (-not (Test-Path $requestPath)) { throw "request missing: $RunId" }
$request = Get-Content $requestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$steps = @(Get-ChildItem (Join-Path $root "steps") -Recurse -Filter result.json -ErrorAction SilentlyContinue | ForEach-Object { Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json })
$required = @($steps | Where-Object { $_.required -eq $true })
$expectedByProfile = @{
    smoke_focus = @("smoke")
    art_runtime = @("ui_spec_validate", "art_acceptance")
    t0_seal = @("config_sync", "ui_spec_validate", "art_manifest_check", "smoke", "t0_capture")
    p0_full = @("config_sync", "config_static_validate", "ui_spec_validate", "smoke")
}
$expected = @($expectedByProfile[$request.profile_id])
$missing = @($expected | Where-Object { $_ -notin @($required.step_id) })
if ($required.status -contains "Failed") { $automation = "Failed" }
elseif ($missing.Count -gt 0) { $automation = "Blocked" }
elseif ($required.Count -eq 0) { $automation = "Blocked" }
elseif ($required.status -contains "Blocked") { $automation = "Blocked" }
elseif ($required.status -contains "Limited") { $automation = "Limited" }
elseif ($required.status -contains "Cancelled") { $automation = "Cancelled" }
else { $automation = "Passed" }
if ($automation -ne "Passed") { $ceiling = "evidence_collected" }
elseif ($OwnerValidation -notin @("Passed")) { $ceiling = "automation_passed" }
elseif ($ExternalReview -eq "Passed" -or $ExternalReview -eq "NotRequired") { $ceiling = if ($ExternalReview -eq "Passed") { "externally_reviewed" } else { "owner_validated" } }
else { $ceiling = "owner_validated" }
$summary = [ordered]@{ schema_version="p3-validation/summary@1"; run_id=$RunId; profile_id=$request.profile_id; generated_at=(Get-Date).ToUniversalTime().ToString("o"); automation_status=$automation; owner_validation=$OwnerValidation; external_review=$ExternalReview; claim_ceiling=$ceiling; missing_required_steps=$missing; steps=$steps }
$jsonPath = Join-Path $root "validation-summary.json"
$tmp = "$jsonPath.tmp"
$summary | ConvertTo-Json -Depth 20 | Set-Content $tmp -Encoding UTF8
Move-Item $tmp $jsonPath -Force
$md = @("# P3 Validation $RunId", "", "- Profile: $($request.profile_id)", "- AutomationStatus: $automation", "- OwnerValidation: $OwnerValidation", "- ExternalReview: $ExternalReview", "- ClaimCeiling: $ceiling") -join "`n"
Set-Content (Join-Path $root "validation-summary.md") $md -Encoding UTF8
$result = [pscustomobject]@{ AutomationStatus=$automation; OwnerValidation=$OwnerValidation; ExternalReview=$ExternalReview; ClaimCeiling=$ceiling; JsonPath=$jsonPath }
if ($PassThru) { $result } else { $result | ConvertTo-Json }
