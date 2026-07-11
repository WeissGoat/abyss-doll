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
Write-Output "[p3-validation] profile/schema fixtures passed"
