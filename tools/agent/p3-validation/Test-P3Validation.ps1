param([string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath)
$ErrorActionPreference = "Stop"

$profilePath = Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json"
$profiles = Get-Content -LiteralPath $profilePath -Raw -Encoding UTF8 | ConvertFrom-Json
$expected = @("smoke_focus", "art_runtime", "t0_seal", "p0_full")

foreach ($id in $expected) {
    if (-not ($profiles.profiles.id -contains $id)) { throw "missing profile: $id" }
}
foreach ($profile in $profiles.profiles) {
    if ([string]::IsNullOrWhiteSpace($profile.version)) { throw "profile version missing: $($profile.id)" }
    if ($profile.editor_control -ne "exclusive_restore") { throw "invalid editor_control: $($profile.id)" }
}

$fixtures = Get-ChildItem (Join-Path $PSScriptRoot "fixtures") -Filter "*.json"
foreach ($fixture in $fixtures) {
    $data = Get-Content $fixture.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($data.schema_version -notmatch '^p3-validation/') { throw "schema missing: $($fixture.Name)" }
    if ([string]::IsNullOrWhiteSpace($data.run_id)) { throw "run_id missing: $($fixture.Name)" }
}

$newRunScript = Join-Path $PSScriptRoot "New-P3ValidationRun.ps1"
$mergeScript = Join-Path $PSScriptRoot "Merge-P3ValidationEvidence.ps1"
$testRoot = Join-Path $RepoRoot "UnityClient/Logs/P3Validation/runs"
$runId = "contract_test_$([guid]::NewGuid().ToString('N'))"
$run = & $newRunScript -ProfileId smoke_focus -RunId $runId -RepoRoot $RepoRoot -PassThru
try {
    $caseDir = Join-Path $run.EvidenceRoot "steps/case"
    New-Item -ItemType Directory -Force $caseDir | Out-Null
    $failed = Get-Content (Join-Path $PSScriptRoot "fixtures/step-failed.json") -Raw | ConvertFrom-Json
    $failed.run_id = $runId
    $failed | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $caseDir "result.json") -Encoding UTF8
    $summary = & $mergeScript -RunId $runId -RepoRoot $RepoRoot -PassThru
    if ($summary.AutomationStatus -ne "Failed") { throw "Failed must outrank Blocked/Limited" }
    if ($summary.ClaimCeiling -ne "evidence_collected") { throw "failed run claim ceiling is too high" }
    $duplicateRejected = $false
    try { & $newRunScript -ProfileId smoke_focus -RunId $runId -RepoRoot $RepoRoot | Out-Null } catch { $duplicateRejected = $true }
    if (-not $duplicateRejected) { throw "duplicate RunId accepted" }
} finally {
    Remove-Item -LiteralPath $run.EvidenceRoot -Recurse -Force -ErrorAction SilentlyContinue
}
$registeredStaticSteps = @("config_sync","config_static_validate","ui_spec_validate","art_manifest_check")
foreach($step in $profiles.profiles.static_steps){ if($step -notin $registeredStaticSteps){throw "unregistered static step in profile: $step"} }
Write-Output "[p3-validation] profile/schema fixtures passed"
