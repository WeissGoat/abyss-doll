param([string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath)
$ErrorActionPreference="Stop"
$profileRoot=Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation"
$program=Get-Content (Join-Path $profileRoot "program_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json
$art=Get-Content (Join-Path $profileRoot "art_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json
$release=Get-Content (Join-Path $profileRoot "release_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json
if($program.profiles.id -contains "art_runtime"){throw "art profile leaked into program registry"}
if($art.profiles.id -contains "p0_full"){throw "program profile leaked into art registry"}
foreach($id in @("smoke_focus","t0_functional","p0_full")){if($id -notin $program.profiles.id){throw "missing program profile: $id"}}
foreach($id in @("art_focus","art_runtime","art_iteration","t0_art_seal")){if($id -notin $art.profiles.id){throw "missing art profile: $id"}}
foreach($id in @("vertical_slice_release","t0_release")){if($id -notin $release.profiles.id){throw "missing release profile: $id"}}
foreach($profile in @($program.profiles)+@($art.profiles)){
    if($profile.version-ne"2"){throw "wrong profile version: $($profile.id)"}
    if(-not $profile.required_steps -or $profile.required_steps.Count-eq 0){throw "required_steps missing: $($profile.id)"}
}
foreach($fixture in Get-ChildItem (Join-Path $PSScriptRoot "fixtures") -Filter *.json){
    $data=Get-Content $fixture.FullName -Raw -Encoding UTF8|ConvertFrom-Json
    if($data.schema_version-ne"p3-validation/step-result@2"){throw "wrong fixture schema: $($fixture.Name)"}
    if($data.validation_domain-notin@("program","art","release","infrastructure")){throw "wrong fixture domain: $($fixture.Name)"}
}
Write-Output "[p3-validation-core] profiles and schemas passed"
