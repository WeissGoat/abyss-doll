param([string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath)
$ErrorActionPreference="Stop"
$profileRoot=Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation"
$program=Get-Content (Join-Path $profileRoot "program_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json
$art=Get-Content (Join-Path $profileRoot "art_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json
$targets=Get-Content (Join-Path $profileRoot "art_validation_targets.json") -Raw -Encoding UTF8|ConvertFrom-Json
$release=Get-Content (Join-Path $profileRoot "release_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json
if($art.schema_version-ne"p3-validation/art-profiles@3"){throw "wrong art profile schema"}
if($targets.schema_version-ne"p3-validation/art-targets@1"){throw "wrong art target schema"}
if($program.profiles.id -contains "art_runtime"){throw "art profile leaked into program registry"}
if($art.profiles.id -contains "p0_full"){throw "program profile leaked into art registry"}
foreach($id in @("smoke_focus","t0_functional","p0_full")){if($id -notin $program.profiles.id){throw "missing program profile: $id"}}
foreach($id in @("art_focus","art_runtime","art_iteration","t0_art_seal","art_regression")){if($id -notin $art.profiles.id){throw "missing art profile: $id"}}
foreach($id in @("vertical_slice_release","t0_release")){if($id -notin $release.profiles.id){throw "missing release profile: $id"}}
foreach($profile in @($program.profiles)){
    if($profile.version-ne"2"){throw "wrong profile version: $($profile.id)"}
    if(-not $profile.required_steps -or $profile.required_steps.Count-eq 0){throw "required_steps missing: $($profile.id)"}
}
foreach($profile in @($art.profiles)){
    if($profile.version-ne"3"){throw "wrong profile version: $($profile.id)"}
    if(-not $profile.required_steps -or $profile.required_steps.Count-eq 0){throw "required_steps missing: $($profile.id)"}
    if($profile.id-ne"art_regression" -and $profile.required_steps -contains "art_acceptance"){throw "legacy runner leaked into daily art profile: $($profile.id)"}
}
if((@($art.profiles|Where-Object {$_.required_steps -contains "art_acceptance"})).Count-ne1){throw "art_acceptance must be exclusive to art_regression"}
$expectedRoots=@{workshop_main="WorkshopPanel";dungeon_map="DungeonMapPanel";dialogue_overlay="P3DialogueOverlay_Runtime";t0_prologue="P3DialogueOverlay_Runtime"}
foreach($id in $expectedRoots.Keys){$target=@($targets.targets|Where-Object {$_.target_id -eq $id});if($target.Count -ne 1){throw "missing or duplicate art target: $id"};if($target[0].expected_roots[0] -ne $expectedRoots[$id]){throw "wrong runtime root: $id"};if($target[0].reference_width -ne 1920 -or $target[0].reference_height -ne 1080){throw "wrong art target reference resolution: $id"}}
$artToolRoot=Join-Path $RepoRoot "UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools"
$unsafeRequests=Get-ChildItem $artToolRoot -Filter "P3Art*Tool.cs"|Select-String -Pattern 'Value<.*>\("(?:source_path|before_path|after_path)"'
if($unsafeRequests){throw "arbitrary screenshot path request parameter found: $($unsafeRequests.Path)"}
foreach($fixture in Get-ChildItem (Join-Path $PSScriptRoot "fixtures") -Filter *.json){
    $data=Get-Content $fixture.FullName -Raw -Encoding UTF8|ConvertFrom-Json
    if($data.schema_version-ne"p3-validation/step-result@2"){throw "wrong fixture schema: $($fixture.Name)"}
    if($data.validation_domain-notin@("program","art","release","infrastructure")){throw "wrong fixture domain: $($fixture.Name)"}
}
Write-Output "[p3-validation-core] profiles and schemas passed"
